using Common;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ProcessingModule
{
    /// <summary>
    /// Class containing logic for automated work.
    /// </summary>
    public class AutomationManager : IAutomationManager, IDisposable
	{
		private Thread automationWorker;
        private AutoResetEvent automationTrigger;
        private IStorage storage;
		private IProcessingManager processingManager;
		private int delayBetweenCommands;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomationManager"/> class.
        /// </summary>
        /// <param name="storage">The storage.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="automationTrigger">The automation trigger.</param>
        /// <param name="configuration">The configuration.</param>
        public AutomationManager(IStorage storage, IProcessingManager processingManager, AutoResetEvent automationTrigger, IConfiguration configuration)
		{
			this.storage = storage;
			this.processingManager = processingManager;
            this.configuration = configuration;
            this.automationTrigger = automationTrigger;
        }

        /// <summary>
        /// Initializes and starts the threads.
        /// </summary>
		private void InitializeAndStartThreads()
		{
			InitializeAutomationWorkerThread();
			StartAutomationWorkerThread();
		}

        /// <summary>
        /// Initializes the automation worker thread.
        /// </summary>
		private void InitializeAutomationWorkerThread()
		{
			automationWorker = new Thread(AutomationWorker_DoWork);
			automationWorker.Name = "Aumation Thread";
		}

        /// <summary>
        /// Starts the automation worker thread.
        /// </summary>
		private void StartAutomationWorkerThread()
		{
			automationWorker.Start();
		}


		private void AutomationWorker_DoWork()
		{
			EGUConverter eguConverter = new EGUConverter();

            PointIdentifier ventil = new PointIdentifier(PointType.DIGITAL_OUTPUT, 2000);
            PointIdentifier grejac = new PointIdentifier(PointType.DIGITAL_OUTPUT, 2002);
			PointIdentifier nivoVode = new PointIdentifier(PointType.ANALOG_OUTPUT, 1000);
            PointIdentifier tempVazduha = new PointIdentifier(PointType.ANALOG_OUTPUT, 1001);

			List<PointIdentifier> pointIds = new List<PointIdentifier>(4) { ventil, grejac, nivoVode, tempVazduha };
            while (!disposedValue)
            {
				List<IPoint> points = storage.GetPoints(pointIds); //ucitavanje vrednosti


				int inicijalnaTemperaturaVazduha = (int)eguConverter.ConvertToEGU(points[3].ConfigItem.ScaleFactor, points[3].ConfigItem.Deviation, points[3].RawValue);
				int trenutnaTemperaturaVazduha = inicijalnaTemperaturaVazduha;
				int inicijalniNivoVode = (int)eguConverter.ConvertToEGU(points[2].ConfigItem.ScaleFactor, points[2].ConfigItem.Deviation, points[2].RawValue);
				int trenutniNivoVode = inicijalniNivoVode;

                int Heating = 0;
				int Threshold = 57; // prag pozara
				int Cooling = 4; //koliko hladi za 1s
                int Outflow = 10; //koliko vode istekne za 1s


                if (points[1].RawValue != 0)
				{
					if(inicijalnaTemperaturaVazduha < 30)
					{
						Heating = 2;

                    }else if (inicijalnaTemperaturaVazduha >= 30 && inicijalnaTemperaturaVazduha <= 50)
					{
						Heating = 5;
					}
					else
					{
                        Heating = 20;
                    }

                    trenutnaTemperaturaVazduha = inicijalnaTemperaturaVazduha + Heating;


                }

				if (trenutnaTemperaturaVazduha > Threshold)
				{
					if (points[1].RawValue != 0)
					{
						processingManager.ExecuteWriteCommand(points[1].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 2002, 0);

					}

					if (points[0].RawValue != 1)
					{
						processingManager.ExecuteWriteCommand(points[0].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 2000, 1);
					}

				}
				

				if (points[0].RawValue == 1 && trenutniNivoVode >= 10)
				{
					trenutniNivoVode -= Outflow;
					trenutnaTemperaturaVazduha -= Cooling;

				}else if (points[0].RawValue == 1 && trenutniNivoVode < 10)
                {
					trenutniNivoVode -= inicijalniNivoVode; //ovo je kada su vrednosti vode od 1 do 9
					trenutnaTemperaturaVazduha = (int)(trenutnaTemperaturaVazduha - (trenutnaTemperaturaVazduha / 10) * Cooling);
                    //ovo je razmera tako sto je 100:10 = x : trenutanNivoVode
                    //x = (100*trenutanNivoVode)/10
                    //zatim procenat od 10 koji imamo u vodi cemo pomnoziti sa Cooling i dobiti novu temp koju treba da oduzmemo

                }

				if (trenutniNivoVode == 0)
				{
					if (points[0].RawValue != 0)
					{
						processingManager.ExecuteWriteCommand(points[0].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 2000, 0);
					}

				}

				if (trenutniNivoVode != inicijalniNivoVode)
				{
					//promena iz egu u raw da bismo mogli da ga upisemo u simulaciju

					trenutniNivoVode = (int)eguConverter.ConvertToRaw(points[2].ConfigItem.ScaleFactor, points[2].ConfigItem.Deviation, trenutniNivoVode);
					processingManager.ExecuteWriteCommand(points[2].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 1000, trenutniNivoVode);
				}


				if (trenutnaTemperaturaVazduha != inicijalnaTemperaturaVazduha)
				{
					//promena iz egu u raw da bismo mogli da ga upisemo u simulaciju

					trenutnaTemperaturaVazduha = (int)eguConverter.ConvertToRaw(points[3].ConfigItem.ScaleFactor, points[3].ConfigItem.Deviation, trenutnaTemperaturaVazduha);
					processingManager.ExecuteWriteCommand(points[3].ConfigItem, configuration.GetTransactionId(), configuration.UnitAddress, 1001, trenutnaTemperaturaVazduha);
				}


                automationTrigger.WaitOne(1000);


            }
        }

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls


        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">Indication if managed objects should be disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
				}
				disposedValue = true;
			}
		}


		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// GC.SuppressFinalize(this);
		}

        /// <inheritdoc />
        public void Start(int delayBetweenCommands)
		{
			this.delayBetweenCommands = delayBetweenCommands*1000;
            InitializeAndStartThreads();
		}

        /// <inheritdoc />
        public void Stop()
		{
			Dispose();
		}
		#endregion
	}
}

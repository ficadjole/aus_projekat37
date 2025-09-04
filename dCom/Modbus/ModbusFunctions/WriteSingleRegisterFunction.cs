using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write single register functions/requests.
    /// </summary>
    public class WriteSingleRegisterFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleRegisterFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleRegisterFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusWriteCommandParameters write = (ModbusWriteCommandParameters)CommandParameters;

            byte[] request = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)write.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)write.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)write.Length)), 0, request, 4, 2);
            request[6] = write.UnitId;
            request[7] = write.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)write.OutputAddress)), 0, request, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)write.Value)), 0, request, 10, 2);


            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> values = new Dictionary<Tuple<PointType, ushort>, ushort>();

            byte[] tempAdress = new byte[2];
            byte[] tempValue = new byte[2];

            tempAdress[0] = response[8];
            tempAdress[1] = response[9];

            ushort address = BitConverter.ToUInt16(new byte[2] { tempAdress[1], tempAdress[0] }, 0);

            tempValue[0] = response[10];
            tempValue[1] = response[11];

            ushort value = BitConverter.ToUInt16(new byte[2] { tempValue[1], tempValue[0] }, 0);

            values.Add(new Tuple<PointType, ushort>(PointType.ANALOG_OUTPUT, address), value);

            return values;
        }
    }
}
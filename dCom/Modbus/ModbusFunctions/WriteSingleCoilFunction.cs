using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus write coil functions/requests.
    /// </summary>
    public class WriteSingleCoilFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteSingleCoilFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public WriteSingleCoilFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusWriteCommandParameters));
        }

        /// <inheritdoc />
        public override byte[] PackRequest()
        {
            ModbusWriteCommandParameters readParams = (ModbusWriteCommandParameters)CommandParameters;

            byte[] request = new byte[12];
            int offset = 0;

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.TransactionId)), 0, request,0, 2);
            offset += 2;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.ProtocolId)), 0, request,0, 2);
            offset += 2;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.Length)), 0, request,4, 2);
            offset += 2;
            request[offset++] = readParams.UnitId;
            request[offset++] = readParams.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.OutputAddress)), 0, request, 8, 2);
            offset += 2;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)readParams.Value)), 0, request, 10, 2);
            offset += 2;

            return request;
            
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            Dictionary<Tuple<PointType, ushort>, ushort> dictionary = new Dictionary<Tuple<PointType, ushort>, ushort>();

            byte[] tempAddress = new byte[2];
            byte[] tempValue = new byte[2];

            tempAddress[0] = response[8];
            tempValue[1] = response[9];

            ushort address = BitConverter.ToUInt16(new byte[2] { tempAddress[1], tempAddress[0] }, 0);
            tempValue[0] = response[10];
            tempValue[1] = response[11];

            ushort value = BitConverter.ToUInt16(new byte[2] { tempValue[1], tempValue[0] }, 0);

            dictionary.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), value);

            return dictionary;
        }
    }
}
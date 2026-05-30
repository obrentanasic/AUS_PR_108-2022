using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read coil functions/requests.
    /// </summary>
    public class ReadCoilsFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadCoilsFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
		public ReadCoilsFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc/>
        public override byte[] PackRequest()
        {
            ModbusReadCommandParameters parameters = this.CommandParameters as ModbusReadCommandParameters;
            byte[] request = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.Length)), 0, request, 4, 2);
            request[6] = parameters.UnitId;
            request[7] = parameters.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.StartAddress)), 0, request, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.Quantity)), 0, request, 10, 2);

            return request;
        }

        /// <inheritdoc />
        public override Dictionary<Tuple<PointType, ushort>, ushort> ParseResponse(byte[] response)
        {
            if ((response[7] & 0x80) != 0)
            {
                HandeException(response[8]);
            }

            Dictionary<Tuple<PointType, ushort>, ushort> pointValues = new Dictionary<Tuple<PointType, ushort>, ushort>();
            ModbusReadCommandParameters parameters = this.CommandParameters as ModbusReadCommandParameters;

            for (int i = 0; i < parameters.Quantity; i++)
            {
                int byteIndex = 9 + (i / 8);
                int bitIndex = i % 8;
                ushort bitValue = (ushort)((response[byteIndex] & (1 << bitIndex)) != 0 ? 1 : 0);
                pointValues.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, (ushort)(parameters.StartAddress + i)), bitValue);
            }

            return pointValues;
        }
    }
}

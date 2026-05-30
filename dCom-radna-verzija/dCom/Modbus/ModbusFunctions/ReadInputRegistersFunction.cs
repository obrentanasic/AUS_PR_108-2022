using Common;
using Modbus.FunctionParameters;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;

namespace Modbus.ModbusFunctions
{
    /// <summary>
    /// Class containing logic for parsing and packing modbus read input registers functions/requests.
    /// </summary>
    public class ReadInputRegistersFunction : ModbusFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReadInputRegistersFunction"/> class.
        /// </summary>
        /// <param name="commandParameters">The modbus command parameters.</param>
        public ReadInputRegistersFunction(ModbusCommandParameters commandParameters) : base(commandParameters)
        {
            CheckArguments(MethodBase.GetCurrentMethod(), typeof(ModbusReadCommandParameters));
        }

        /// <inheritdoc />
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
                ushort value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 9 + i * 2));
                pointValues.Add(new Tuple<PointType, ushort>(PointType.ANALOG_INPUT, (ushort)(parameters.StartAddress + i)), value);
            }

            return pointValues;
        }
    }
}

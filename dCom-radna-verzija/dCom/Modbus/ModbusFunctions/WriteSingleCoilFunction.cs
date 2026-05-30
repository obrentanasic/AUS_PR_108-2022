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
            ModbusWriteCommandParameters parameters = this.CommandParameters as ModbusWriteCommandParameters;
            byte[] request = new byte[12];

            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.TransactionId)), 0, request, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.ProtocolId)), 0, request, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.Length)), 0, request, 4, 2);
            request[6] = parameters.UnitId;
            request[7] = parameters.FunctionCode;
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)parameters.OutputAddress)), 0, request, 8, 2);
            // Coil ON is encoded as 0xFF00, coil OFF as 0x0000 (Modbus spec).
            ushort coilValue = (ushort)(parameters.Value != 0 ? 0xFF00 : 0x0000);
            Buffer.BlockCopy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)coilValue)), 0, request, 10, 2);

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
            ushort address = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 8));
            ushort value = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(response, 10));
            // Map the echoed coil value (0xFF00 / 0x0000) back to a logical 1 / 0 for the UI.
            ushort coilState = (ushort)(value != 0 ? 1 : 0);
            pointValues.Add(new Tuple<PointType, ushort>(PointType.DIGITAL_OUTPUT, address), coilState);

            return pointValues;
        }
    }
}

using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using NModbus;

namespace Aplicativo_DashBoard.Comunicacao
{
    public class ModbusTcpClient
    {
        private TcpClient? _tcpClient;
        private IModbusMaster? _master;

        // =========================================================
        // STATUS DA CONEXÃO
        // =========================================================

        public bool IsConnected
        {
            get
            {
                return _tcpClient != null &&
                       _tcpClient.Connected;
            }
        }

        // =========================================================
        // CONEXÃO TCP
        // =========================================================

        public async Task ConnectAsync(string ip, int porta)
        {
            Disconnect();

            _tcpClient = new TcpClient();

            await _tcpClient.ConnectAsync(ip, porta);

            var factory = new ModbusFactory();

            _master = factory.CreateMaster(_tcpClient);
        }

        // =========================================================
        // DESCONECTAR
        // =========================================================

        public void Disconnect()
        {
            try
            {
                _master = null;

                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient.Dispose();
                    _tcpClient = null;
                }
            }
            catch
            {
                _tcpClient = null;
                _master = null;
            }
        }

        // =========================================================
        // LEITURA DE HOLDING REGISTERS
        // =========================================================

        public ushort[] ReadHoldingRegisters(
            byte unitId,
            ushort startAddress,
            ushort quantity)
        {
            if (!IsConnected || _master == null)
                throw new InvalidOperationException(
                    "O XP300 não está conectado.");

            return _master.ReadHoldingRegisters(
                unitId,
                startAddress,
                quantity);
        }

        // =========================================================
        // LEITURA DE COILS
        // =========================================================

        public bool[] ReadCoils(
            byte unitId,
            ushort startAddress,
            ushort quantity)
        {
            if (!IsConnected || _master == null)
                throw new InvalidOperationException(
                    "O XP300 não está conectado.");

            return _master.ReadCoils(
                unitId,
                startAddress,
                quantity);
        }

        // =========================================================
        // ESCRITA DE UM COIL
        // FC05 - WRITE SINGLE COIL
        // =========================================================

        public void WriteSingleCoil(
            byte unitId,
            ushort address,
            bool value)
        {
            if (!IsConnected || _master == null)
                throw new InvalidOperationException(
                    "O XP300 não está conectado.");

            _master.WriteSingleCoil(
                unitId,
                address,
                value);
        }
    }
}
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Aplicativo_DashBoard.Comunicacao;

namespace Aplicativo_DashBoard
{
    public partial class MainWindow : Window
    {
        private readonly ModbusTcpClient _modbus;
        private readonly DispatcherTimer _timerLeitura;

        public MainWindow()
        {
            InitializeComponent();

            _modbus = new ModbusTcpClient();

            _timerLeitura = new DispatcherTimer();
            _timerLeitura.Interval = TimeSpan.FromSeconds(1);
            _timerLeitura.Tick += TimerLeitura_Tick;
        }


        // =========================================================
        // CONEXÃO COM O XP300
        // =========================================================

        private async void BtnConectar_Click(
            object sender,
            RoutedEventArgs e)
        {
            string ip = TxtIp.Text.Trim();

            if (!int.TryParse(
                TxtPorta.Text.Trim(),
                out int porta))
            {
                TxtStatusConexao.Text = "Porta inválida";
                TxtStatusCabecalho.Text = "Erro";

                LedStatus.Fill =
                    new SolidColorBrush(Colors.Red);

                return;
            }

            try
            {
                BtnConectar.IsEnabled = false;
                BtnConectar.Content = "CONECTANDO...";

                _timerLeitura.Start();

                TxtStatusConexao.Text =
                    "Conectando ao XP300...";

                TxtStatusCabecalho.Text =
                    "Conectando";

                LedStatus.Fill =
                    new SolidColorBrush(Colors.Gold);

                await _modbus.ConnectAsync(ip, porta);

                TxtStatusConexao.Text =
                    "Conectado ao XP300";

                TxtStatusCabecalho.Text =
                    "Conectado";

                LedStatus.Fill =
                    new SolidColorBrush(Colors.LimeGreen);

                BtnConectar.Content =
                    "CONECTADO";
            }
            catch (Exception ex)
            {
                _modbus.Disconnect();

                TxtStatusConexao.Text =
                    "Falha: " + ex.Message;

                TxtStatusCabecalho.Text =
                    "Desconectado";

                LedStatus.Fill =
                    new SolidColorBrush(Colors.Red);

                BtnConectar.Content =
                    "CONECTAR";
            }
            finally
            {
                BtnConectar.IsEnabled = true;
            }
        }

        //==========================================================
        // CONVERSÃO DE DADOS
        //==========================================================
        // CORRENTES
        private static string DiagnosticarReal(
        ushort registro1,
        ushort registro2)
        {
            byte[] normal =
            {
        (byte)(registro1 >> 8),
        (byte)registro1,
        (byte)(registro2 >> 8),
        (byte)registro2
    };

            byte[] invertido =
            {
        (byte)(registro2 >> 8),
        (byte)registro2,
        (byte)(registro1 >> 8),
        (byte)registro1
    };

            return
                $"Normal: {BitConverter.ToSingle(normal, 0):G9}\n" +
                $"Invertido: {BitConverter.ToSingle(invertido, 0):G9}";
        }


        // =========================================================
        // LEITURA AUTOMÁTICA
        // =========================================================

        private void TimerLeitura_Tick(
            object? sender,
            EventArgs e)
        {
            if (_modbus.IsConnected)
            {
                BtnLerPextron_Click(
                    null!,
                    new RoutedEventArgs());
            }
        }


        // =========================================================
        // LEITURA DOS REGISTROS DO XP300
        // =========================================================

        private void BtnLerPextron_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (!_modbus.IsConnected)
                {
                    MessageBox.Show(
                        "Primeiro conecte ao XP300.",
                        "Modbus TCP");

                    return;
                }

                // =====================================================
                // REGISTROS 100 A 102
                // =====================================================

                ushort[] correntesInt = _modbus.ReadHoldingRegisters(
                        1,   // Unit ID
                        99,  // Endereço NModbus correspondente ao TCP 100
                        3);  // TCP 100, 101 e 102

                // =====================================================
                // REGISTROS 80 A 97
                // =====================================================

                ushort[] valores =
                    _modbus.ReadHoldingRegisters(
                        1,      // Unit ID
                        79,     // Registro inicial NModbus
                        22);     // Quantidade


                // =====================================================
                // CORRENTES - REAL de 32 bits
                // TCP 80-81 = Corrente C
                // TCP 82-83 = Corrente B
                // TCP 84-85 = Corrente A
                // =====================================================

                short correnteCInt = unchecked((short)correntesInt[0]);
                short correnteBInt = unchecked((short)correntesInt[1]);
                short correnteAInt = unchecked((short)correntesInt[2]);

                double correnteC = correnteCInt / 100.0;
                double correnteB = correnteBInt / 100.0;
                double correnteA = correnteAInt / 100.0;

                TxtCorrenteFaseA.Text = $"{correnteA:F2} A";
                TxtCorrenteFaseB.Text = $"{correnteB:F2} A";
                TxtCorrenteFaseC.Text = $"{correnteC:F2} A";


                // =====================================================
                // TENSÕES
                //
                // 86 = Tensão C
                // 87 = Tensão B
                // 88 = Tensão A
                // =====================================================

                TxtTensaoFaseA.Text =
                    valores[8].ToString() + " V";

                TxtTensaoFaseB.Text =
                    valores[7].ToString() + " V";

                TxtTensaoFaseC.Text =
                    valores[6].ToString() + " V";


                // =====================================================
                // POTÊNCIAS ATIVAS
                //
                // 89 = Potência C
                // 90 = Potência B
                // 91 = Potência A
                // 
                // Registradores INT de 16 bits com sinal
                // =====================================================

                short potenciaA = unchecked((short)valores[11]);
                short potenciaB = unchecked((short)valores[10]);
                short potenciaC = unchecked((short)valores[9]);

                TxtPotenciaAtivaA.Text =
                    potenciaA.ToString() + " kW";

                TxtPotenciaAtivaB.Text =
                    potenciaB.ToString() + " kW";

                TxtPotenciaAtivaC.Text =
                    potenciaC.ToString() + " kW";

                // =====================================================
                // FATOR DE POTÊNCIA
                // TCP 93-94 = Fator de Potência C
                // TCP 95-96 = Fator de Potência B
                // TCP 97-98 = Fator de Potência A
                // =====================================================

                TxtFatorPotenciaA.Text =
                    $"{valores[16]}.{valores[17]}";

                TxtFatorPotenciaB.Text =
                    $"{valores[14]}.{valores[15]}";

                TxtFatorPotenciaC.Text =
                    $"{valores[12]}.{valores[13]}";

                // =====================================================
                // PROTEÇÃO 27
                //
                // Mapa TCP:
                //
                // 35 = 27C
                // 36 = 27B
                // 37 = 27A
                //
                // NModbus começa em zero:
                // endereço inicial = 34
                // =====================================================

                bool[] trip27 =
                    _modbus.ReadCoils(
                        1,      // Unit ID
                        34,     // NModbus -> TCP 35
                        3);     // 35, 36 e 37


                bool trip27C = trip27[0];
                bool trip27B = trip27[1];
                bool trip27A = trip27[2];

                bool algumaFalha27 =
                    trip27A ||
                    trip27B ||
                    trip27C;


                // =====================================================
                // PROTEÇÃO 32
                //
                // Mapa TCP:
                //
                // 43 = 32C
                // 44 = 32B
                // 45 = 32A
                //
                // NModbus:
                // endereço inicial = 42
                // =====================================================

                bool[] trip32 =
                    _modbus.ReadCoils(
                        1,      // Unit ID
                        42,     // NModbus -> TCP 43
                        3);     // 43, 44 e 45


                bool trip32C = trip32[0];
                bool trip32B = trip32[1];
                bool trip32A = trip32[2];

                bool algumaFalha32 = trip32A || trip32B || trip32C;

                // =====================================================
                // PROTEÇÃO 37
                //
                // Mapa TCP:
                //
                // 51 = 37C
                // 52 = 37B
                // 53 = 37A
                //
                // NModbus:
                // endereço inicial = 50
                // =====================================================

                bool[] trip37 =
               _modbus.ReadCoils(
                    1,      // Unit ID
                    50,     // TCP 51 -> NModbus 50
                    3);     // 51, 52 e 53

                bool trip37C = trip37[0];
                bool trip37B = trip37[1];
                bool trip37A = trip37[2];
                bool algumaFalha37 = trip37A || trip37B || trip37C;

                // =====================================================
                // PROTEÇÃO 50
                //
                // Mapa TCP:
                //
                // 46 = 50N
                // 47 = 50C
                // 48 = 50B
                // 49 = 50A
                //
                // NModbus:
                // endereço inicial = 45
                // =====================================================
                bool[] trip50 =
               _modbus.ReadCoils(
                    1,      // Unit ID
                    45,     // TCP 46 -> NModbus 45
                    4);     // 46, 47, 48 e 49

                bool trip50N = trip50[0];
                bool trip50C = trip50[1];
                bool trip50B = trip50[2];
                bool trip50A = trip50[3];
                bool algumaFalha50 = trip50A || trip50B || trip50C || trip50N;
                // =====================================================
                // PROTEÇÃO 51
                //
                // Mapa TCP:
                //
                // 38 = 51N
                // 39 = 51C
                // 40 = 51B
                // 41 = 51A
                //
                // NModbus:
                // endereço inicial = 37
                // =====================================================
                bool[] trip51 =
               _modbus.ReadCoils(
                    1,      // Unit ID
                    37,     // TCP 38 -> NModbus 37
                    4);     // 38, 39, 40 e 41

                bool trip51N = trip51[0];
                bool trip51C = trip51[1];
                bool trip51B = trip51[2];
                bool trip51A = trip51[3];
                bool algumaFalha51 = trip51A || trip51B || trip51C || trip51N;

                // =====================================================
                // PROTEÇÃO 59
                //
                // Mapa TCP:
                //
                // 62 = 59N
                // 63 = 59C
                // 64 = 59B
                // 65 = 59A
                //
                // NModbus:
                // endereço inicial = 61
                // =====================================================
                bool[] trip59 = 
                    _modbus.ReadCoils(
                         1,      // Unit ID
                         61,     // TCP 62 -> NModbus 61
                         4);     // 62, 63, 64 e 65

                bool trip59N = trip59[0];
                bool trip59C = trip59[1];
                bool trip59B = trip59[2];
                bool trip59A = trip59[3];
                bool algumaFalha59 = trip59A || trip59B || trip59C || trip59N;

                // =====================================================
                // PROTEÇÃO 67
                //
                // Mapa TCP:
                //
                // 54 = 67N
                // 55 = 67C
                // 56 = 67B
                // 57 = 67A
                //
                // NModbus:
                // endereço inicial = 53
                // =====================================================
                bool[] trip67 =
               _modbus.ReadCoils(
                    1,      // Unit ID
                    53,     // TCP 54 -> NModbus 53
                    4);     // 54, 55, 56 e 57

                bool trip67N = trip67[0];
                bool trip67C = trip67[1];
                bool trip67B = trip67[2];
                bool trip67A = trip67[3];
                bool algumaFalha67 = trip67A || trip67B || trip67C || trip67N;

                // =====================================================
                // PROTEÇÃO 47, 78 e 86
                //
                // Mapa TCP:
                //
                // 67 = 78
                // 68 = 86
                // 69 = 47
                //
                // NModbus:
                // endereço inicial = 66
                // =====================================================
                bool[] trip47_78_86 =
               _modbus.ReadCoils(
                    1,      // Unit ID
                    66,     // TCP 67 -> NModbus 66
                    3);     // 67,68 e 69

                bool trip78 = trip47_78_86[0];
                bool trip86 = trip47_78_86[1];
                bool trip47 = trip47_78_86[2];
                bool algumaFalha47_78_86 = trip47||trip78||trip86;

                // =====================================================
                // PROTEÇÃO GS, Q e 81
                //
                // Mapa TCP:
                //
                // 59 = GS
                // 60 = Q
                // 61 = 81
                //
                // NModbus:
                // endereço inicial = 58
                // =====================================================
                bool[] tripGS_Q_81 =
               _modbus.ReadCoils(
                    1,      // Unit ID
                    58,     // TCP 59 -> NModbus 58
                    3);     // 59,60 e 61

                bool tripGS = tripGS_Q_81[0];
                bool tripQ = tripGS_Q_81[1];
                bool trip81 = tripGS_Q_81[2];
                bool algumaFalhaGS_Q_81 = trip81 || tripGS || tripQ;


                // =====================================================
                // RESET DO RELÉ
                //
                // Habilita se houver qualquer TRIP 
                // =====================================================

                bool algumaFalhaReset = ( algumaFalha27 || algumaFalha32 || algumaFalha37 || algumaFalha50 || 
                algumaFalha51 || algumaFalha59 || algumaFalha67 || algumaFalhaGS_Q_81 || algumaFalha47_78_86);

                BtnResetRele.IsEnabled =
                    algumaFalhaReset;


                if (algumaFalhaReset)
                {
                    BtnResetRele.Background =
                        new SolidColorBrush(Colors.Gold);

                    BtnResetRele.Foreground =
                        new SolidColorBrush(Colors.Black);
                }
                else
                {
                    BtnResetRele.Background =
                        new SolidColorBrush(
                            Color.FromRgb(85, 85, 85));

                    BtnResetRele.Foreground =
                        new SolidColorBrush(Colors.White);
                }


                // =====================================================
                // ATUALIZAÇÃO VISUAL - PROTEÇÃO 27
                // =====================================================

                AtualizarTrip(
                    LedTrip27A,
                    TxtTrip27A,
                    trip27A);

                AtualizarTrip(
                    LedTrip27B,
                    TxtTrip27B,
                    trip27B);

                AtualizarTrip(
                    LedTrip27C,
                    TxtTrip27C,
                    trip27C);


                // =====================================================
                // ATUALIZAÇÃO VISUAL - PROTEÇÃO 32
                // =====================================================

                AtualizarTrip(LedTrip32A,TxtTrip32A,trip32A);
                AtualizarTrip(LedTrip32B,TxtTrip32B,trip32B);
                AtualizarTrip(LedTrip32C,TxtTrip32C, trip32C);

                // =====================================================
                // ATUALIZAÇÃO VISUAL - PROTEÇÃO 37
                // =====================================================

                AtualizarTrip(LedTrip37A, TxtTrip37A, trip37A);
                AtualizarTrip(LedTrip37B, TxtTrip37B, trip37B);
                AtualizarTrip(LedTrip37C, TxtTrip37C, trip37C);

                // =====================================================
                // ATUALIZAÇÃO VISUAL - PROTEÇÃO 50
                // =====================================================

                AtualizarTrip(LedTrip50A, TxtTrip50A, trip50A);
                AtualizarTrip(LedTrip50B, TxtTrip50B, trip50B);
                AtualizarTrip(LedTrip50C, TxtTrip50C, trip50C);
                AtualizarTrip(LedTrip50N, TxtTrip50N, trip50N);

                // =====================================================
                // ATUALIZAÇÃO VISUAL - PROTEÇÃO 51
                // =====================================================

                AtualizarTrip(LedTrip51A, TxtTrip51A, trip51A);
                AtualizarTrip(LedTrip51B, TxtTrip51B, trip51B);
                AtualizarTrip(LedTrip51C, TxtTrip51C, trip51C);
                AtualizarTrip(LedTrip51N, TxtTrip51N, trip51N);

                // =====================================================
                // ATUALIZAÇÃO VISUAL - PROTEÇÃO 59
                // =====================================================

                AtualizarTrip(LedTrip59A, TxtTrip59A, trip59A);
                AtualizarTrip(LedTrip59B, TxtTrip59B, trip59B);
                AtualizarTrip(LedTrip59C, TxtTrip59C, trip59C);
                AtualizarTrip(LedTrip59N, TxtTrip59N, trip59N);

                // =====================================================
                // ATUALIZAÇÃO VISUAL - PROTEÇÃO 67
                // =====================================================

                AtualizarTrip(LedTrip67A, TxtTrip67A, trip67A);
                AtualizarTrip(LedTrip67B, TxtTrip67B, trip67B);
                AtualizarTrip(LedTrip67C, TxtTrip67C, trip67C);
                AtualizarTrip(LedTrip67N, TxtTrip67N, trip67N);

                // =====================================================
                // ATUALIZAÇÃO VISUAL - PROTEÇÃO 47,78,81,86,GS,Q
                // =====================================================

                AtualizarTrip(LedTrip47, TxtTrip47, trip47);
                AtualizarTrip(LedTrip78, TxtTrip78, trip78);
                AtualizarTrip(LedTrip81, TxtTrip81, trip81);
                AtualizarTrip(LedTrip86, TxtTrip86, trip86);
                AtualizarTrip(LedTripGS, TxtTripGS, tripGS);
                AtualizarTrip(LedTripQ, TxtTripQ, tripQ);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Erro na leitura do XP300");
            }
        }


        // =========================================================
        // ATUALIZAÇÃO VISUAL DOS LEDS DE PROTEÇÃO
        // =========================================================

        private void AtualizarTrip(
            Ellipse led,
            TextBlock texto,
            bool ativo)
        {
            if (ativo)
            {
                // ---------------------------------------------
                // PROTEÇÃO ATIVA
                // ---------------------------------------------

                led.Fill =
                    new SolidColorBrush(Colors.Gold);

                texto.Foreground =
                    new SolidColorBrush(Colors.White);

                texto.FontWeight =
                    FontWeights.Bold;
            }
            else
            {
                // ---------------------------------------------
                // PROTEÇÃO NORMAL
                // ---------------------------------------------

                led.Fill =
                    new SolidColorBrush(
                        Color.FromRgb(107, 114, 128));

                texto.Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(156, 163, 175));

                texto.FontWeight =
                    FontWeights.Normal;
            }
        }


        // =========================================================
        // RESET DO RELÉ PEXTRON
        // =========================================================

        private async void BtnResetRele_Click(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                if (!_modbus.IsConnected)
                {
                    MessageBox.Show(
                        "O XP300 não está conectado.",
                        "RESET RELÉ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                // =================================================
                // MAPA TCP DO XP300
                //
                // 77 = Cmd_Reset_Remoto
                //
                // NModbus começa em zero:
                // 77 -> endereço 76
                // =================================================

                ushort enderecoReset = 76;


                // =================================================
                // ATIVA O RESET
                // =================================================

                _modbus.WriteSingleCoil(
                    1,
                    enderecoReset,
                    true);


                // =================================================
                // MANTÉM O COMANDO ATIVO POR 3,5 SEGUNDOS
                // =================================================

                await Task.Delay(3500);


                // =================================================
                // DESATIVA O RESET
                // =================================================

                _modbus.WriteSingleCoil(
                    1,
                    enderecoReset,
                    false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro na escrita TCP:\n\n{ex.Message}",
                    "RESET RELÉ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
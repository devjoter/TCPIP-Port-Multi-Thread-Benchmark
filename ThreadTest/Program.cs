using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;


namespace ThreadTest
{
    class Program
    {
        static object _locker = new object();//Inicjalizacja obiektu do obslugi lock
        static Mutex _mutex = new Mutex();//Inicjalizacja obiektu obslugi Muex
        static object _monitor = new object();//Inicjalizacja obiektu obslugi Monitor

        static ThreadLocal<Stopwatch> _localStopwatch = new ThreadLocal<Stopwatch>(() => new Stopwatch());//Przechowuje dane z timera osobno w kazdym watku                    
        static string IP = "";//wskazanie adresu do skanowania
        static void Main(string[] args)
        {
            try
            {
                CreateDirectory();//Metoda do utworzenia folderu na dysku
                Console.WriteLine("Program will scan TCP ports in range input by user");//dane wejsciowe
                UserInput(out int a, out int b);
                Console.WriteLine("Provide IP adress or url to scan...");
                IP = Console.ReadLine();


                Console.WriteLine("Press 1 for Mutex muttithread synchronisation test\n\r" +
                                    "Press 2 for Lock multithread synchronisation test\n\r" +
                                    "Press 3 for Monitor multithread synchronisation test\n\r" +
                                    "Press 4 for Single thread test");

                switch (Console.ReadKey(true).Key)//wybor menu 
                {

                    case ConsoleKey.D1:
                        StartMutex(a, b);
                        break;
                    case ConsoleKey.D2:
                        StartLock(a, b);
                        break;
                    case ConsoleKey.D3:
                        StartMonitor(a, b);
                        break;
                    case ConsoleKey.D4:
                        StartOneThread(a, b);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("Program will save the file at C:/Watki/ with test result.  Press any key to extit after scanning is done !!!");
                Console.ReadKey(true);
            }
            
        }
       
        #region Metody obslugi watkow       
        static void StartMutex(int a, int b)//Metoda slozy do wystartowania watkow w przypisanym zakresie potrtow
        {                         
            for (int i = a; i <= b; i++)
            {               
                int port = i;//przypisanie zniennej aby adres pamieci wskazywal na jedno miejsce podczas wykonywania kazdego watku, 
                             //bez tego kilka watkow moglo by wskazywac na ta sama wartosc zmiennej. Zabieg jest zstosowany aby 
                             //metoda w wyrazeniu ponizej mogla przekazac parametr (int port)

                
                Thread t = new Thread(() => {

                    //Wystartowanie watkow w petli, mozna uzyc parametru jako delgata do ParameterizedThreadStart jednakze tutaj jest bardziej czytelnie
                    //biorac pod uwage jeszcze funkcje i parametry do testowania predkosci ich dzialania . 

                    _localStopwatch.Value.Reset();//obsluga timera
                    _localStopwatch.Value.Start();
                    PortScanMultiThreadMutex(port);//Metoda skanujaca port
                    _localStopwatch.Value.Stop();
                    Console.WriteLine($"Time elapsed : {_localStopwatch.Value.Elapsed.TotalMilliseconds} msec");//debug czasu do konsoli
                    string fileName = @"c:\Watki\MutexDebug.txt";
                    Write(fileName, a, b);//zapisanie pomiaru czasu do pliku
                    

                });
                t.Name = $"Mutex nb: {i}";//nadanie watkom nazw
                t.Start();//wystartowanie watka                
            }            
        }
        
        static void StartLock(int a, int b)//Tak jak powyzej, obsuga Lock
        {
            for (int i = a; i <= b; i++)
            {
                int port = i;
                Thread t = new Thread(() => {
                    _localStopwatch.Value.Reset();
                    _localStopwatch.Value.Start();
                    PortScanMultiThreadLock(port);
                    _localStopwatch.Value.Stop();
                    Console.WriteLine($"Time elapsed : {_localStopwatch.Value.Elapsed.TotalMilliseconds} msec");
                    string fileName = @"c:\Watki\LockDebug.txt";
                    Write(fileName, a, b);
                });
                t.Name = $" Lock nb: {i}";
                t.Start();
            }
           
        }
        static void StartMonitor(int a, int b)//Tak jak powyzej, obsluga Monitor
        {

            for (int i = a; i <= b; i++)
            {
                int port = i;
                Thread t = new Thread(() => {
                    _localStopwatch.Value.Reset();
                    _localStopwatch.Value.Start();
                    PortScanMultiThreadMonitor(port);
                    _localStopwatch.Value.Stop();
                    Console.WriteLine($"Time elapsed : {_localStopwatch.Value.Elapsed.TotalMilliseconds} msec");
                    string fileName = @"c:\Watki\MonitorDebug.txt";
                    Write(fileName, a, b);
                    
                });
                t.Name = $"Monitor nb: {i}";
                t.Start();
            }
        }
        static void StartOneThread(int a, int b)//
        {
            for (int i = a; i <= b; i++)
            {
                int port = i;
                //_localStopwatch.Value.Reset();
                _localStopwatch.Value.Start();
                PortStartOneThread(port);
                _localStopwatch.Value.Stop();
                Console.WriteLine($"Time elapsed : {_localStopwatch.Value.Elapsed.TotalMilliseconds} msec");
                string fileName = @"c:\Watki\SingleThreadDebug.txt";
                Write(fileName, a, b);
                

            }
        }
        #endregion

        #region Metody zadan dla watkow, obiekty synchronizacji 
        static void Scaner(TcpClient ScanTCP, string IP, int port)//definicja skanera bazujacego na klasie TCPClient
        {            
            try
            {
                ScanTCP.Connect(IP, port);//Inicjalizacja polaczenia
                Console.ForegroundColor = ConsoleColor.Green;//zmiana koloru debug
                Console.Write($"TCP {port} PORT OPEN |");//debug
                Console.ForegroundColor = ConsoleColor.White;//powrot koloru konsoli do bialego
                //Console.WriteLine($" Checked by Thread {Thread.CurrentThread.Name} done...");
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;//zmiana koloru tekstu
                Console.Write($"TCP {port} PORT CLOSED |");//debug
                Console.ForegroundColor = ConsoleColor.White;//powrot koloru
                //Console.WriteLine($" Checked by Thread {Thread.CurrentThread.Name} done...");
            }
        }
        static void PortScanMultiThreadMutex(int port)//Metoda wykorzystujaca funkcje Scanner, do przeskanowania portu TCP uzywajac TcpClient 
        {
            try
            {
                //Console.WriteLine($"Thread {Thread.CurrentThread.Name} begin...");//debug
                _mutex.WaitOne();//zablokowanie obiektu synchronizacji watkow Mutex
                //Console.WriteLine($"Thread {Thread.CurrentThread.Name} start...");//debug
                using (TcpClient Scan = new TcpClient())//w bloku using mam pewnosc ze polaczenie zostanie zakonczone
                {
                    Scaner(Scan, IP, port);                                                          
                }
                _mutex.ReleaseMutex();//odblokowanie obiektu synchronizacji watkow Mutex                
            }
            catch (Exception ex)//obsluga wyjatkow
            {
                Console.WriteLine(ex);//debug
            }            
        }

        static void PortScanMultiThreadLock(int port)//Metoda wykorzystujaca funkcje Scanner, do przeskanowania portu TCP uzywajac TcpClient
        {
            //Console.WriteLine($"Thread {Thread.CurrentThread.Name} begin...");
            lock (_locker)//zablokowanie sekcji krytycznej synchronizacji watkow Lock 
            {
                //Console.WriteLine($"Thread {Thread.CurrentThread.Name} start...");                
                using (TcpClient Scan = new TcpClient())
                {
                    Scaner(Scan, IP, port);
                }
            }
           
        }

        static void PortScanMultiThreadMonitor(int port)
        {
            //Console.WriteLine($"Thread {Thread.CurrentThread.Name} begin...");
            Monitor.Enter(_monitor);//zablokowanie sekcji krytycznej synchronizacji watkow Monitor
            //Console.WriteLine($"Thread {Thread.CurrentThread.Name} start...");
            try
            {
                using (TcpClient Scan = new TcpClient())
                {
                    Scaner(Scan, IP, port);
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex);
            }
            finally
            {
                Monitor.Exit(_monitor);
            }
        }

        static void PortStartOneThread(int port)//Metoda skanowania portu w jednym watku
        {           
            using (TcpClient Scan = new TcpClient())
            {
                Scaner(Scan, IP, port);
            }
        }


        #endregion

        #region Helpers

        static void UserInput(out int a, out int b)//Definicja metody do pobierania danych wejsciowych
        {
            Console.Write("Start Port: ");
            a = int.Parse(Console.ReadLine());
            Console.Write("End Port: ");
            b = int.Parse(Console.ReadLine());
        }
        static async void Write(string fileName, int a, int b)//Metoda slozaca do zapisu pliku z wynikiem testu, uzyta w ThreadLocal
        {
            
            using (StreamWriter writer = File.CreateText(fileName))//dzieki uzyciu using przy klasie StreamWriter obiekt zostaje usuniety po zakonczeniu dzialania
            {

                await writer.WriteLineAsync($"Scanning adress: {IP} done on port range {a} to {b}\n\r " +
                $"Time Elapsed {_localStopwatch.Value.Elapsed.TotalMilliseconds} mili sec");//asynchroniczny zapis danych do pliku
            }
        }
        static void CreateDirectory()//Metoda do obsugi utworzenia pliku tekstowego
        {
            string directoryPath = @"c:\Watki\";
            if (!Directory.Exists(directoryPath))
            {
                Console.Write("Creating directory..");
                Directory.CreateDirectory(directoryPath);
            }
            
        }
        #endregion
    }
}

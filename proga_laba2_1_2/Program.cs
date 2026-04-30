using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Lab2_DataSetProcessing
{
    class Program
    {
        // Список для хранения результатов обработки
        private static readonly List<string> _resultsLog = new List<string>();
        private static readonly object _logLock = new object();

        // Общий итог по всем наборам
        private static long _totalSum = 0;
        private static readonly Mutex _totalSumMutex = new Mutex();

        // Семафор для ограничения количества одновременно работающих потоков
        private static SemaphoreSlim _semaphore;

        private const int DataSetCount = 15;
        private const int NumbersPerSet = 100;
        private const int MaxThreads = 5;
        private const string DataFileName = "datasets.csv";

        static void Main()
        {
            Console.WriteLine("=== ЛАБОРАТОРНАЯ РАБОТА №2. ЗАДАНИЕ 1.2 ===\n");
            Console.WriteLine("Обработка наборов чисел с ограничением числа потоков");
            Console.WriteLine($"Наборов данных: {DataSetCount}");
            Console.WriteLine($"Чисел в наборе: {NumbersPerSet}");
            Console.WriteLine($"Максимум потоков одновременно: {MaxThreads}\n");

            // Генерируем и сохраняем наборы данных
            GenerateDataSets();

            // Читаем наборы из файла
            List<List<int>> dataSets = ReadDataSets();

            // Инициализируем семафор
            _semaphore = new SemaphoreSlim(MaxThreads, MaxThreads);

            // Замер времени
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Создаём и запускаем потоки для каждого набора
            Thread[] threads = new Thread[DataSetCount];

            for (int i = 0; i < DataSetCount; i++)
            {
                int setIndex = i;
                List<int> dataSet = dataSets[i];

                threads[i] = new Thread(() => ProcessDataSet(setIndex + 1, dataSet));
                threads[i].Start();
            }

            // Ожидаем завершения всех потоков
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            stopwatch.Stop();

            // Вывод результатов
            Console.WriteLine("\n=== РЕЗУЛЬТАТЫ ОБРАБОТКИ ===\n");

            lock (_logLock)
            {
                foreach (string entry in _resultsLog)
                {
                    Console.WriteLine(entry);
                }
            }

            Console.WriteLine($"\nОбщий итог по всем наборам: {_totalSum}");
            Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
            Console.WriteLine("\n=== РАБОТА ЗАВЕРШЕНА ===");
            Console.ReadKey();
        }

        /// <summary>
        /// Генерация наборов данных и сохранение в CSV-файл
        /// </summary>
        static void GenerateDataSets()
        {
            if (File.Exists(DataFileName))
            {
                Console.WriteLine($"Файл '{DataFileName}' уже существует. Используем готовые данные.\n");
                return;
            }

            Console.WriteLine($"Генерируем {DataSetCount} наборов по {NumbersPerSet} случайных чисел...");

            Random random = new Random(42); // Фиксированный seed для воспроизводимости

            using (StreamWriter writer = new StreamWriter(DataFileName))
            {
                for (int set = 0; set < DataSetCount; set++)
                {
                    List<int> numbers = new List<int>();
                    for (int i = 0; i < NumbersPerSet; i++)
                    {
                        numbers.Add(random.Next(1, 101)); // От 1 до 100
                    }
                    writer.WriteLine(string.Join(",", numbers));
                }
            }

            Console.WriteLine($"Данные сохранены в файл '{DataFileName}'.\n");
        }

        /// <summary>
        /// Чтение наборов данных из CSV-файла
        /// </summary>
        static List<List<int>> ReadDataSets()
        {
            List<List<int>> dataSets = new List<List<int>>();

            string[] lines = File.ReadAllLines(DataFileName);

            foreach (string line in lines)
            {
                List<int> dataSet = new List<int>();
                string[] parts = line.Split(',');

                foreach (string part in parts)
                {
                    dataSet.Add(int.Parse(part));
                }

                dataSets.Add(dataSet);
            }

            Console.WriteLine($"Прочитано {dataSets.Count} наборов из файла.\n");
            return dataSets;
        }

        /// <summary>
        /// Обработка одного набора данных
        /// </summary>
        static void ProcessDataSet(int setNumber, List<int> dataSet)
        {
            // Ждём разрешения от семафора
            _semaphore.Wait();
            try
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine($"[Поток {threadId}] Начата обработка набора #{setNumber}");

                // Вычисляем сумму
                long sum = 0;
                foreach (int number in dataSet)
                {
                    sum += number;
                }

                // Записываем результат в общий журнал (используем lock)
                string resultEntry = $"Набор #{setNumber:D2}: сумма = {sum}, поток = {threadId}";
                lock (_logLock)
                {
                    _resultsLog.Add(resultEntry);
                }

                // Обновляем общий итог (используем Mutex)
                _totalSumMutex.WaitOne();
                try
                {
                    _totalSum += sum;
                }
                finally
                {
                    _totalSumMutex.ReleaseMutex();
                }

                Console.WriteLine($"[Поток {threadId}] Завершена обработка набора #{setNumber}. Сумма = {sum}");
            }
            finally
            {
                // Освобождаем семафор
                _semaphore.Release();
            }
        }
    }
}
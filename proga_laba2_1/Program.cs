using System;
using System.Diagnostics;
using System.Threading;

namespace proga_laba2_1
{
    class Program
    {
        // Общий счётчик простых чисел
        private static int _primeCount = 0;

        // Mutex для защиты общего счётчика
        private static readonly Mutex _mutex = new Mutex();

        // Параметры
        private const int MinNumber = 1;
        private const int MaxNumber = 10000;
        private const int ThreadCount = 4;

        static void Main()
        {
            Console.WriteLine("=== ЛАБОРАТОРНАЯ РАБОТА №2. ЗАДАНИЕ 1.1 ===\n");
            Console.WriteLine("Подсчёт простых чисел в диапазоне от 1 до 10 000");
            Console.WriteLine($"Количество потоков: {ThreadCount}");
            Console.WriteLine("Механизм синхронизации: Mutex\n");

            // Замер времени
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Создаём потоки
            Thread[] threads = new Thread[ThreadCount];
            int rangePerThread = (MaxNumber - MinNumber + 1) / ThreadCount;

            for (int i = 0; i < ThreadCount; i++)
            {
                int threadIndex = i;
                int start = MinNumber + threadIndex * rangePerThread;
                int end = (threadIndex == ThreadCount - 1) ? MaxNumber : start + rangePerThread - 1;

                threads[i] = new Thread(() => FindPrimesInRange(threadIndex + 1, start, end));
                threads[i].Start();
            }

            // Ожидаем завершения всех потоков
            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            stopwatch.Stop();

            Console.WriteLine($"\nОбщее количество простых чисел: {_primeCount}");
            Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");
            Console.WriteLine("\n=== РАБОТА ЗАВЕРШЕНА ===");
            Console.ReadKey();
        }

        /// <summary>
        /// Поиск простых чисел в заданном диапазоне
        /// </summary>
        static void FindPrimesInRange(int threadNumber, int start, int end)
        {
            Console.WriteLine($"[Поток {threadNumber}] Обрабатывает диапазон: {start} - {end}");

            int localCount = 0;

            for (int number = start; number <= end; number++)
            {
                Console.WriteLine($"[Поток {threadNumber}] Обрабатывается число: {number}");

                if (IsPrime(number))
                {
                    Console.WriteLine($"[Поток {threadNumber}] >>> Найдено простое число: {number}");

                    // Захватываем Mutex перед обновлением общего счётчика
                    _mutex.WaitOne();
                    try
                    {
                        _primeCount++;
                    }
                    finally
                    {
                        // Освобождаем Mutex в любом случае
                        _mutex.ReleaseMutex();
                    }

                    localCount++;
                }
            }

            Console.WriteLine($"[Поток {threadNumber}] Завершён. Найдено простых в диапазоне: {localCount}");
        }

        /// <summary>
        /// Проверка числа на простоту
        /// </summary>
        static bool IsPrime(int number)
        {
            if (number < 2)
                return false;

            if (number == 2)
                return true;

            if (number % 2 == 0)
                return false;

            int limit = (int)Math.Sqrt(number);
            for (int i = 3; i <= limit; i += 2)
            {
                if (number % i == 0)
                    return false;
            }

            return true;
        }
    }
}
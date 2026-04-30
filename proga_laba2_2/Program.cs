using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace Lab2_AsyncHttpRequests
{
    class Program
    {
        // Три публичных API, возвращающих JSON
        private static readonly string[] Urls = new string[]
        {
            "https://jsonplaceholder.typicode.com/posts/1",
            "https://jsonplaceholder.typicode.com/users/1",
            "https://jsonplaceholder.typicode.com/comments/1"
        };

        static async Task Main()
        {
            Console.WriteLine("=== ЛАБОРАТОРНАЯ РАБОТА №2. ЗАДАНИЕ №2 ===\n");
            Console.WriteLine("Асинхронное выполнение HTTP-запросов\n");
            Console.WriteLine("Версия 2 — с использованием async/await\n");
            Console.WriteLine($"Будет выполнено запросов: {Urls.Length}");
            Console.WriteLine("Запросы выполняются АСИНХРОННО (одновременно)\n");

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                // Выполняем все три запроса асинхронно и ждём завершения всех
                string[] results = await FetchAllUrlsAsync();

                stopwatch.Stop();

                // Вывод результатов
                Console.WriteLine("=== РЕЗУЛЬТАТЫ ===\n");

                for (int i = 0; i < Urls.Length; i++)
                {
                    Console.WriteLine(new string('─', 60));
                    Console.WriteLine($"Ответ от сервера {i + 1}:");
                    Console.WriteLine($"URL: {Urls[i]}");
                    Console.WriteLine(new string('─', 60));
                    
                    // Выводим первые 300 символов JSON (чтобы не занимать всю консоль)
                    string preview = results[i].Length > 300 
                        ? results[i].Substring(0, 300) + "..." 
                        : results[i];
                    Console.WriteLine(preview);
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ОШИБКА: {ex.Message}");
            }

            Console.WriteLine($"\nОбщее время выполнения: {stopwatch.ElapsedMilliseconds} мс");
            Console.WriteLine("\n=== РАБОТА ЗАВЕРШЕНА ===");
            Console.ReadKey();
        }

        /// <summary>
        /// Асинхронно выполняет запросы ко всем URL одновременно.
        /// Использует Task.WhenAll для ожидания завершения всех запросов.
        /// </summary>
        static async Task<string[]> FetchAllUrlsAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                // Увеличиваем таймаут для надёжности
                client.Timeout = TimeSpan.FromSeconds(30);

                Console.WriteLine("Запуск асинхронных запросов...\n");

                // Создаём задачи для каждого URL
                Task<string> task1 = FetchJsonAsync(client, Urls[0], 1);
                Task<string> task2 = FetchJsonAsync(client, Urls[1], 2);
                Task<string> task3 = FetchJsonAsync(client, Urls[2], 3);

                Console.WriteLine("Все три запроса отправлены одновременно.");
                Console.WriteLine("Программа НЕ блокируется — другие операции могут выполняться.");
                Console.WriteLine("Ожидание ответов...\n");

                // Ждём завершения ВСЕХ задач одновременно
                string[] results = await Task.WhenAll(task1, task2, task3);

                return results;
            }
        }

        /// <summary>
        /// Асинхронно выполняет HTTP-запрос и возвращает JSON-строку.
        /// </summary>
        /// <param name="client">HTTP-клиент</param>
        /// <param name="url">URL для запроса</param>
        /// <param name="requestNumber">Номер запроса (для вывода в консоль)</param>
        static async Task<string> FetchJsonAsync(HttpClient client, string url, int requestNumber)
        {
            try
            {
                Console.WriteLine($"[Запрос {requestNumber}] Отправка запроса к: {url}");

                // Асинхронно отправляем GET-запрос (НЕ блокирует поток!)
                HttpResponseMessage response = await client.GetAsync(url);

                // Проверяем успешность ответа
                response.EnsureSuccessStatusCode();

                Console.WriteLine($"[Запрос {requestNumber}] Ответ получен (статус: {response.StatusCode})");

                // Асинхронно читаем содержимое ответа как строку
                string json = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[Запрос {requestNumber}] Данные прочитаны ({json.Length} символов)");

                return json;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[Запрос {requestNumber}] ❌ Ошибка сети: {ex.Message}");
                return $"{{\"error\": \"{ex.Message}\"}}";
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"[Запрос {requestNumber}] ❌ Таймаут запроса");
                return $"{{\"error\": \"timeout\"}}";
            }
        }
    }
}
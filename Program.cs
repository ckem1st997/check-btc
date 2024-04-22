using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NBitcoin;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using NetTopologySuite.Index.HPRtree;


namespace test_btc
{
    internal class Program
    {
        static async Task Main(string[] args)
        {

            Console.WriteLine("Input number thread:");
            int number = int.Parse(Console.ReadLine());

            // Biến để cache dữ liệu
            List<string> cachedData = new List<string>();
            HashSet<string> addData = new HashSet<string>();
            //  await AddData(Path.Combine(Environment.CurrentDirectory, "btc-list-address.txt"), addData);
            List<string> data = Wordlist.English.GetWords().ToList();
            // Tạo ra 5 Task để chạy hàm Check
            Task[] tasks = new Task[number < 1 ? 1 : number];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    // Gọi hàm Check và đợi kết quả
                    await Check(data, addData);
                });
            }

            // Đợi cho tất cả các Task hoàn thành
            await Task.WhenAll(tasks);

            Console.WriteLine("Tất cả các luồng đã hoàn thành.");

            static int[] GetRandomNumbers(DateTime dateTime, int[] randomNumbers)
            {
                // Sử dụng dữ liệu từ DateTime để tạo seed cho Random
                int seed = (int)(dateTime.Millisecond + dateTime.Second * 1000 + dateTime.Minute * 60000 + dateTime.Hour * 3600000 +
                           dateTime.Day * 86400000 + dateTime.Month * 2678400000 + dateTime.Year * 31536000000);
                // Tạo đối tượng Random với seed từ DateTime
                Random random = new Random(seed);
                // Tạo 12 số ngẫu nhiên và đưa vào mảng
                for (int i = 0; i < 12; i++)
                {
                    randomNumbers[i] = random.Next(2048);
                }

                return randomNumbers;
            }

            static async Task Check(List<string> words, HashSet<string> addDataCheck)
            {

                string currentDirectory = Environment.CurrentDirectory;
                List<string> rd = new List<string>();
                string mnemonicWords = "";
                int count = 0;
                int seedNum = 12;

                Random random = new Random();

                //
                DateTime dateTime = new DateTime(2000, 1, 1);
                int[] randomNumbers = new int[12];
                while (true)
                {
                    mnemonicWords = string.Empty;

                    var nums = GetRandomNumbers(dateTime, randomNumbers);
                    foreach (var item in nums)
                    {
                        mnemonicWords = mnemonicWords + " " + words[item];
                    }
                    dateTime = dateTime.AddSeconds(1);
                    //rd = new List<string>();
                    //var listRd = new List<int>();
                    //for (int i = 0; i < seedNum; i++)
                    //{
                    //    bool b = true;
                    //    while (b)
                    //    {
                    //        int randomIndex = random.Next(2048);
                    //        var check = listRd.Where(x => x == randomIndex);
                    //        if ((check == null || !check.Any()))
                    //        {
                    //            rd.Add(randomIndex.ToString());
                    //            listRd.Add(randomIndex);
                    //            mnemonicWords = mnemonicWords + " " + words[randomIndex];
                    //            b = false;
                    //        }
                    //    }

                    //}

                    mnemonicWords = mnemonicWords.Trim();

                    // if (!(!string.IsNullOrEmpty(mnemonicWords) && (mnemonicWords.Split(" ").Length == 12 || mnemonicWords.Split(" ").Length == 24))) continue;
                    if (!(!string.IsNullOrEmpty(mnemonicWords) && (mnemonicWords.Split(" ").Length == 12))) continue;
                    try
                    {
                        count++;
                        Mnemonic mnemonic = new Mnemonic(mnemonicWords, Wordlist.English);
                        // Tạo master key từ mnemonic
                        ExtKey masterKey = mnemonic.DeriveExtKey();

                        // KeyPath cho mỗi loại địa chỉ
                        KeyPath keyPathSegwit = new KeyPath("m/84'/0'/0'/0/0"); // P2WPKH
                        KeyPath keyPathLegacy = new KeyPath("m/44'/0'/0'/0/0"); // P2PKH
                        KeyPath keyPathP2SH = new KeyPath("m/49'/0'/0'/0/0"); // P2SH-P2WPKH

                        var listAddress = new List<string>();
                        var extKey = masterKey.Derive(keyPathSegwit);
                        var address = extKey.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);

                        listAddress.Add(address.ToString());
                        var extKey1 = masterKey.Derive(keyPathLegacy);
                        var addres1s = extKey1.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);

                        listAddress.Add(addres1s.ToString());
                        var extKey2 = masterKey.Derive(keyPathP2SH);
                        var address2 = extKey2.PrivateKey.PubKey.GetAddress(ScriptPubKeyType.SegwitP2SH, Network.Main);
                        listAddress.Add(address2.ToString());
                        Console.WriteLine($"[{count}]|{Task.CurrentId}-{address}");
                        // Tạo và kiểm tra các loại địa chỉ khác nhau
                        // Stopwatch stopwatch = Stopwatch.StartNew();
                        try
                        {
                            // Tạo địa chỉ từ master key và key path
                            // Kiểm tra xem địa chỉ có trong file CSV không
                            bool addressFound = false;
                            foreach (var VARIABLE in listAddress)
                            {
                                if (addDataCheck.Contains(VARIABLE))
                                {
                                    addressFound = true;
                                    break;
                                }
                            }
                            if (addressFound)
                            {
                                string output = $"12 Seed: {mnemonicWords} | address:{String.Join(", ", listAddress)}";
                                string filePath = Path.Combine(Environment.CurrentDirectory, "btc-wallet.txt");

                                await using (StreamWriter sw = File.AppendText(filePath))
                                {
                                    await sw.WriteLineAsync(output);
                                }
                                Console.WriteLine($"Thông tin đã được ghi vào file cho địa chỉ: {String.Join(", ", listAddress)}");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("\nException Caught!");
                            Console.WriteLine("Message :{0} ", e.Message);
                        }


                        //stopwatch.Stop();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("\nException Caught!");
                        Console.WriteLine("Message :{0} ", e.Message);
                    }
                }
            }




            static async Task AddData(string csvFilePath, HashSet<string> addDataCheck)
            {
                string? line = "";
                if (addDataCheck.Count < 1)
                {
                    Console.WriteLine("begin aync data !");
                    using (var reader = new StreamReader(csvFilePath))
                    {
                        // Đọc từng dòng trong tệp
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            if (!string.IsNullOrEmpty(line))
                                addDataCheck.Add(line);
                        }
                        reader.Close();
                        reader.Dispose();
                    }
                    Console.WriteLine("end aync data !");
                    Console.WriteLine("data: " + addDataCheck.Count);
                }
            }

        }
    }
}
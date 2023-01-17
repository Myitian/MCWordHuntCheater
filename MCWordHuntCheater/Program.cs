using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Myitian.MCWordHuntCheater
{
    internal class Program
    {
        const int WORD_LEN = 5;
        const int TAKE_COUNT = 10;
        const string EXAMPLE_CORRECT = "s ee";
        const string EXAMPLE_INCORRECT = "d t";
        const string EXAMPLE_EXCLUDE = "rapoinluckydns";
        static void Main()
        {
            try
            {
                HashSet<string> words = new HashSet<string>();
                StringBuilder sb = new StringBuilder();
                string wsha1 = "";
                if (File.Exists("words.txt"))
                {
                    byte[] wordstxt_bytes = File.ReadAllBytes("words.txt");
                    byte[] sha1 = SHA1.Create().ComputeHash(wordstxt_bytes);
                    foreach (byte b in sha1)
                    {
                        sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
                    }
                    wsha1 = sb.ToString();
                }
                do
                {
                    if (File.Exists("cached_words.txt"))
                    {
                        Console.WriteLine("# Cached word list found.");
                        Console.WriteLine("# Reading cached word list ...");

                        using (FileStream fs = new FileStream("cached_words.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (StreamReader reader = new StreamReader(fs, Encoding.ASCII))
                            {
                                string csha1 = reader.ReadLine();
                                if (csha1 != wsha1)
                                {
                                    Console.WriteLine("# Original word list is modified.");
                                    Console.WriteLine("# Rebuilding word list ...");
                                }
                                else
                                {
                                    string line = reader.ReadLine();
                                    while (line != null)
                                    {
                                        words.Add(line);
                                        line = reader.ReadLine();
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("# Cached word list not found.");
                        Console.WriteLine("# Building word list ...");
                    }
                    if (wsha1 != "")
                    {
                        using (FileStream fs = new FileStream("words.txt", FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            using (StreamReader reader = new StreamReader(fs, Encoding.ASCII))
                            {
                                string line = reader.ReadLine();
                                while (line != null)
                                {
                                    if (line.Length == WORD_LEN)
                                    {
                                        line = line.ToLower();
                                        bool formcorrect = true;
                                        for (int i = 0; formcorrect && i < WORD_LEN; i++)
                                        {
                                            char c = line[i];
                                            if (c < 'a' || c > 'z') formcorrect = false;
                                        }
                                        if (formcorrect) words.Add(line);
                                    }
                                    line = reader.ReadLine();
                                }
                            }
                        }

                        using (FileStream fs = new FileStream("cached_words.txt", FileMode.Create, FileAccess.Write, FileShare.Read))
                        {
                            using (StreamWriter writer = new StreamWriter(fs, Encoding.ASCII))
                            {
                                writer.WriteLine(wsha1);
                                foreach (string word in words)
                                {
                                    writer.WriteLine(word);
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException("words.txt");
                    }
                } while (false);
                Console.WriteLine("# Done!");
                Console.WriteLine();
                Console.WriteLine("===== Guide =====");

                Console.WriteLine("*** Input - Correct");
                Console.WriteLine("[a-zA-Z] = the letter is at the correct spot");
                Console.WriteLine("\\+\\d+ = add offset");
                Console.WriteLine("\\-\\d+ = remove offset");
                Console.WriteLine("=\\d+ = set offset");
                Console.WriteLine("*** Input - Incorrect");
                Console.WriteLine("[a-zA-Z] = the letter is at the incorrect spot");
                Console.WriteLine("*** Input - Exclude");
                Console.WriteLine("[a-zA-Z] = the letter is not in the word");

                Console.WriteLine("*** Example:");
                Console.WriteLine("Correct  >>>" + EXAMPLE_CORRECT);
                string guess_correct = EXAMPLE_CORRECT.ToLower().PadRight(WORD_LEN).Substring(0, WORD_LEN);
                Console.WriteLine("Incorrect>>>" + EXAMPLE_INCORRECT);
                string guess_incorrect = EXAMPLE_INCORRECT.ToLower().PadRight(WORD_LEN).Substring(0, WORD_LEN);
                Console.WriteLine("Exclude  >>>" + EXAMPLE_EXCLUDE);
                string guess_exclude = EXAMPLE_EXCLUDE.ToLower();
                IEnumerable<string> possibleresult = words.Where(x => IsWordMatch(x, guess_correct, guess_incorrect, guess_exclude));
                string[] taked = possibleresult.Take(TAKE_COUNT).ToArray();
                Console.WriteLine($"{taked.Length} of {possibleresult.Count()} possible results:");
                Console.WriteLine(string.Join("\r\n", taked));

                Console.WriteLine("===== ----- =====");

                possibleresult = null;

                int offset = 0;
                int readint = 0;
                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("Enter word:");
                    Console.Write("Correct  >>>");
                    string readline = Console.ReadLine().ToLower().PadRight(WORD_LEN);
                    switch (readline[0])
                    {
                        case '+':
                        case '-':
                            if (possibleresult is null)
                            {
                                Console.WriteLine("# Results are not generated.");
                                continue;
                            }
                            else if (!int.TryParse(readline, out readint) || offset + readint < 0 || offset + readint > possibleresult.Count())
                            {
                                Console.WriteLine("# Not a valid offset number.");
                            }
                            else
                            {
                                offset += readint;
                            }
                            break;
                        case '=':
                            if (possibleresult is null)
                            {
                                Console.WriteLine("# Results are not generated.");
                                continue;
                            }
                            else if (!int.TryParse(readline.Substring(1), out readint) || readint < 0 || readint > possibleresult.Count())
                            {
                                Console.WriteLine("# Not a valid offset number.");
                            }
                            else
                            {
                                offset = readint;
                            }
                            break;
                        default:
                            guess_correct = readline.Substring(0, WORD_LEN);
                            Console.Write("Incorrect>>>");
                            guess_incorrect = Console.ReadLine().ToLower().PadRight(WORD_LEN).Substring(0, WORD_LEN);
                            Console.Write("Exclude  >>>");
                            guess_exclude = Console.ReadLine().ToLower();
                            possibleresult = words.Where(x => IsWordMatch(x, guess_correct, guess_incorrect, guess_exclude));
                            offset = 0;
                            break;
                    }
                    taked = possibleresult.Skip(offset).Take(TAKE_COUNT).ToArray();
                    if(taked.Length > 0)
                    {
                        Console.WriteLine($"[{offset},{offset + taked.Length - 1}] of {possibleresult.Count()} possible results:");
                        Console.WriteLine(string.Join("\r\n", taked));
                    }
                    else
                    {
                        Console.WriteLine("0 possible results.");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.WriteLine();
                Console.WriteLine("# Press any key to continue ...");
                Console.ReadKey();
            }
        }

        public static bool IsWordMatch(string word, string guess_correct, string guess_incorrect, string guess_exclude)
        {
            bool[] correct_mask = new bool[WORD_LEN];
            int[] incorrect_mask = new int[WORD_LEN];
            HashSet<char> nonexistent_letters = new HashSet<char>(guess_exclude);

            for (int i = 0; i < WORD_LEN; i++)
            {
                char wc = word[i];

                char cc = guess_correct[i];
                if (cc >= 'a' && cc <= 'z')
                {
                    if (wc != cc) return false;
                    correct_mask[i] = true;
                }

                char ic = guess_incorrect[i];
                if (ic >= 'a' && ic <= 'z')
                {
                    if (wc == ic) return false;
                    incorrect_mask[i] = ic;
                }
            }

            for (int i = 0; i < WORD_LEN; i++)
            {
                int m = incorrect_mask[i];
                if (m == 0) continue;
                bool match = false;
                for (int j = 0; j < WORD_LEN; j++)
                {
                    if (i != j && !correct_mask[j] && m == word[j])
                    {
                        match = true;
                        correct_mask[j] = true;
                        break;
                    }
                }
                if (!match) return false;
            }

            for (int i = 0; i < WORD_LEN; i++)
            {
                if (!correct_mask[i] && nonexistent_letters.Contains(word[i])) return false;
            }
            return true;
        }
    }
}
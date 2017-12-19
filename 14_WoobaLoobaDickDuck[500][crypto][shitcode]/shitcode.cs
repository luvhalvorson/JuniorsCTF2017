using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace crypt
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var alphabet = Enumerable.Range(0, 255).ToList();
            var key = String.Empty.ToCharArray();
            
            try
            {
                if (( args.Length >= 1 ) && ( ( args[0] == "-enc" ) || ( args[0] == "-cr" ) ))
                {
                    bool mode = args[0] != "-enc";

                    var buf = Enumerable.Range(' ', 'z' - ' ' + 1).Where(i => i < 42).Select(i => (char) i).ToArray();
                    using (var ks = new StreamReader(args.Length >= 2 ? args[1] : "key"))
                    {
                        while (ks.Peek() >= 0)
                        {
                            ks.ReadBlock(buf, 0, buf.Length);
                            key = Regex.Replace(String.Concat(buf), @"[^\w]", "", RegexOptions.Compiled).ToCharArray();
                        }
                    }

                    var rs = new StreamReader(args.Length >= 3 ? args[2] : "input");
                    var ws = new StreamWriter(args.Length >= 4 ? args[3] : "output");

                    var rRule = Enumerable.Range(' ', 'z' - ' ' + 1).ToDictionary(
                            i => (char) i,
                            i =>
                            {
                                var res = alphabet[( key[i % key.Length] + key[( i + key.Length / 2 ) % key.Length] ) % alphabet.Count];
                                alphabet.Remove(res);
                                return (char) res;
                            });

                    while (rs.Peek() >= 0)
                    {
                        buf = new string(' ', buf.Length).ToCharArray();
                        rs.ReadBlock(buf, 0, buf.Length);
                        buf = buf.Select(
                                c => ( mode
                                        ? rRule
                                        : rRule.ToDictionary(r => r.Value, r => r.Key) ).ContainsKey(c)
                                            ? (
                                            mode
                                                ? rRule
                                                : rRule.ToDictionary(r => r.Value, r => r.Key) )[c]
                                            : c).ToArray();

                        
                        Enumerable.Range(0, key.Length / 2).Aggregate(
                            Enumerable.Range(0, 0), (ints, i) => 
                                
                                ints.Concat(new[]{
                                Enumerable.Range(key.Length/2, key.Length/2).Except(ints).ToArray()[
                                                                         (key[i%key.Length] + key[(i + key.Length/2)%key.Length])
                                                                         %Enumerable.Range(key.Length/2,key.Length/2).Except(ints).Count()
                                                                     ]
                                }),

                                ints => ints.Except(Enumerable.Range(0, key.Length / 2))
                                ).Select((value, index) => new[] { value, index }).ToDictionary(ints => ints.Last(), ints => ints.First()).ToList().ForEach(
                                pair =>
                                {
                                    var tmp = buf[pair.Key];
                                    buf[pair.Key] = buf[pair.Value];
                                    buf[pair.Value] = tmp;
                                });

                        ws.Write(buf);
                    }
                    rs.Dispose();
                    rs.Close();

                    ws.Dispose();
                    ws.Close();

                    Console.WriteLine("Success!");
                }
                else
                {
                    Console.WriteLine("Usage: -option [key] [input] [output]");
                    Console.WriteLine("Options: -enc to encrypt, -cr to crypt");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}

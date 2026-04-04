using System;
using System.IO;
using System.Collections.Generic;

namespace Levenshtein
{
	static class Program
	{
		private const string sep = "~";
        private static Dictionary<Tuple<char, char>, float> replacementCosts;
		static void Main(string[] args)
		{
            if (args.Length == 0)
            {
              Console.WriteLine("A dataset should be specified as the first argument");
            }
            else
            {
                string source = args[0];
                string target = args[1];
                string costFileName = args[2];
                replacementCosts = ReadReplacementCosts(costFileName);
                if (!Directory.Exists(target))
                {
                    Directory.CreateDirectory(target);
                }
                foreach (string infile in Directory.GetFiles(source))
                {
                    string name = Path.GetFileName(infile);
                    if (name.Contains("word.train") || name.Contains("word.dev"))
                    {
                        string outfile = Path.Combine(target, name);
                        if (!File.Exists(outfile))
                        {
                            Console.WriteLine(name);
                            using (StreamReader sr = new StreamReader(infile))
                            {
                                using (StreamWriter sw = new StreamWriter(outfile))
                                {
                                    string line;
                                    while ((line = sr.ReadLine()) != null)
                                    {
                                        string[] split = line.Split('\t');
                                        string word = split[0];
                                        if (word != String.Empty)
                                        {
                                            string segmentation = split[1].Replace(" @@", "@");
                                            if (segmentation.Contains(sep))
                                            {
                                                throw new ArgumentException(line);
                                            }
                                            string[] aligned = LevenshteinAlignment(word, segmentation);
                                            if (String.Join("", aligned) != segmentation)
                                            {
                                                Console.WriteLine(
                                                  "Incorrect alignment: {0:20} {1:30} for {2}",
                                                  word, String.Join(" ", aligned), segmentation
                                                );
                                            }
                                            string joined = String.Join(sep, aligned);
                                            if (split.Length == 3)
                                            {
                                              string features = split[2];
                                              sw.WriteLine(word + "\t" + joined + "\t" + features);
                                            }
                                            else
                                            {
                                              sw.WriteLine(word + "\t" + joined);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
		}
		static Dictionary<Tuple<char, char>, float> ReadReplacementCosts(string fileName)
        {
            var replacementCosts = new Dictionary<Tuple<char, char>, float>();
            using (StreamReader sr = new StreamReader(fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] split = line.Split(' ');
                    char firstChar = split[0][0];
                    char secondChar = split[1][0];
                    if (firstChar > secondChar)
                    {
                        throw new ArgumentException("The character pairs should be sorted alphabetically.");
                    }
                    float cost = float.Parse(split[2]);
                    var key = Tuple.Create(firstChar, secondChar);
                    replacementCosts.Add(key, cost);
                }
            }
            return replacementCosts;
        }
        private static float getReplacementCost(char first, char second)
        {
            Tuple<char, char> key = first < second
                ? Tuple.Create(first, second)
                : Tuple.Create(second, first);
            if (replacementCosts.ContainsKey(key))
            {
                return replacementCosts[key];
            }
            else
            {
                return Convert.ToInt32(first != second);
            }
        }
		static string[] LevenshteinAlignment(string a, string b)
		{
			float[,] m = new float[a.Length, b.Length];
			int[,] o = new int[a.Length, b.Length];
			m[0, 0] = 0;
			o[0, 0] = 0;
			for (int i = 1; i < a.Length; i++)
			{
				m[i, 0] = i;
				o[i, 0] = -1;
			}
			for (int j = 1; j < b.Length; j++)
			{
				m[0, j] = j;
				o[0, j] = 1;
			}
			for (int i = 1; i < a.Length; i++)
			{
				for (int j = 1; j < b.Length; j++)
				{
					float fromInsertion = m[i, j - 1] + 1;
					float fromDeletion = m[i - 1, j] + 1;
					float fromMatch = m[i - 1, j - 1] + getReplacementCost(a[i], b[j]);
					if (fromMatch < fromDeletion)
					{
						if (fromMatch < fromInsertion)
						{
							m[i, j] = fromMatch;
							o[i, j] = 0;
						}
						else if (fromDeletion < fromInsertion)
						{
							m[i, j] = fromDeletion;
							o[i, j] = -1;
						}
						else
						{
							m[i, j] = fromInsertion;
							o[i, j] = 1;
						}
					}
					else if (fromDeletion < fromInsertion)
					{
						m[i, j] = fromDeletion;
						o[i, j] = -1;
					}
					else
					{
						m[i, j] = fromInsertion;
						o[i, j] = 1;
					}
				}
			}
			string[] c = new string[a.Length];
			{
				int i = a.Length - 1;
				int j = b.Length - 1;
				while (i >= 0 || j >= 0)
				{
					int op = o[i, j];
					if (op == 1)
					{
						c[i] = b[j] + c[i];
						j--;
					}
					else if (op == -1)
					{
						c[i] = String.Empty;
						i--;
					}
					else
					{
						c[i] = b[j] + c[i];
						i--;
						j--;
					}
				}
			}
			return c;
		}
	}
}

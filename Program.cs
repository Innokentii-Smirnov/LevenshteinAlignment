using System;
using System.IO;

namespace Levenshtein
{
	static class Program
	{
		private const string sep = "~";
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
                                            string joined = String.Join(sep, aligned);
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
		static string[] LevenshteinAlignment(string a, string b)
		{
			int[,] m = new int[a.Length, b.Length];
			int[,] o = new int[a.Length, b.Length];
			m[0, 0] = Convert.ToInt32(a[0] != b[0]);
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
					int fromInsertion = m[i, j - 1] + 1;
					int fromDeletion = m[i - 1, j] + 1;
					int fromMatch = m[i - 1, j - 1] + Convert.ToInt32(a[i] != b[j]);
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

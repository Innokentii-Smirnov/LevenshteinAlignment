using System;
using System.IO;
using System.Collections.Generic;

namespace Levenshtein
{
	static class Program
	{
		private const string sep = "~";
        private const string replacementCostFileName = "Replacements.txt";
        private const string insertionCostFileName = "Insertions.txt";
        private const string deletionCostFileName = "Deletions.txt";
        private const string characterClassFileName = "CharacterClasses.txt";
        private static Dictionary<Tuple<char, char>, int> replacementCosts;
        private static Dictionary<char, int> insertionCosts;
        private static Dictionary<char, int> deletionCosts;
        private static Dictionary<char, char> characterClasses;
        private static string costDirectory;
        private static string ReplacementCostFilePath
        {
            get
            {
                return Path.Combine(costDirectory, replacementCostFileName);
            }
        }
        private static string InsertionCostFilePath
        {
            get
            {
                return Path.Combine(costDirectory, insertionCostFileName);
            }
        }
        private static string DeletionCostFilePath
        {
          get
          {
            return Path.Combine(costDirectory, deletionCostFileName);
          }
        }
        private static string CharacterClassFilePath
        {
          get
          {
            return Path.Combine(costDirectory, characterClassFileName);
          }
        }
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
                costDirectory = args[2];
                replacementCosts = ReadReplacementCosts(ReplacementCostFilePath);
                insertionCosts = ReadCharacterToCostMapping(InsertionCostFilePath);
                deletionCosts = ReadCharacterToCostMapping(DeletionCostFilePath);
                characterClasses = ReadCharacterToClassMapping(CharacterClassFilePath);
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
		static Dictionary<Tuple<char, char>, int> ReadReplacementCosts(string fileName)
        {
            var replacementCosts = new Dictionary<Tuple<char, char>, int>();
            if (!File.Exists(fileName))
            {
                return replacementCosts;
            }
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
                    int cost = int.Parse(split[2]);
                    var key = Tuple.Create(firstChar, secondChar);
                    replacementCosts.Add(key, cost);
                }
            }
            return replacementCosts;
        }
        static Dictionary<char, int> ReadCharacterToCostMapping(string fileName)
        {
            var characterToCost = new Dictionary<char, int>();
            if (!File.Exists(fileName))
            {
                return characterToCost;
            }
            using (StreamReader sr = new StreamReader(fileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] split = line.Split(" ");
                    char character = split[0][0];
                    int cost = int.Parse(split[1]);
                    characterToCost.Add(character, cost);
                }
            }
            return characterToCost;
        }
        static Dictionary<char, char> ReadCharacterToClassMapping(string fileName)
        {
          var characterToClass = new Dictionary<char, char>();
          using (StreamReader sr = new StreamReader(fileName))
          {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
              string[] split = line.Split(" ");
              char character = split[0][0];
              char characterClass = split[1][0];
              characterToClass.Add(character, characterClass);
            }
          }
          return characterToClass;
        }
        private static int getReplacementCost(char first, char second)
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
                if (first == second)
                {
                   return 0;
                }
                else
                {
                    return getCharacterClass(first) == getCharacterClass(second) ? 10 : 100;
                }
            }
        }
        private static int getInsertionCost(char character)
        {
            if (insertionCosts.ContainsKey(character))
            {
                return insertionCosts[character];
            }
            else
            {
                return 10;
            }
        }
        private static int getDeletionCost(char character)
        {
          if (deletionCosts.ContainsKey(character))
          {
            return deletionCosts[character];
          }
          else
          {
            return 10;
          }
        }
        private static int getCharacterClass(char character)
        {
          if (characterClasses.ContainsKey(character))
          {
            return characterClasses[character];
          }
          else
          {
            throw new ArgumentException(
              String.Format("No class is specified for the character {0}.", character)
            );
          }
        }
		static string[] LevenshteinAlignment(string a, string b)
		{
			int[,] m = new int[a.Length, b.Length];
			int[,] o = new int[a.Length, b.Length];
			m[0, 0] = 0;
			o[0, 0] = 0;
			for (int i = 1; i < a.Length; i++)
			{
				m[i, 0] = m[i - 1, 0] + getDeletionCost(a[i]);
				o[i, 0] = -1;
			}
			for (int j = 1; j < b.Length; j++)
			{
				m[0, j] = m[0, j - 1] + getInsertionCost(b[j]);
				o[0, j] = 1;
			}
			for (int i = 1; i < a.Length; i++)
			{
				for (int j = 1; j < b.Length; j++)
				{
					int fromInsertion = m[i, j - 1] + getInsertionCost(b[j]);
					int fromDeletion = m[i - 1, j]  + getDeletionCost(a[i]);
					int fromMatch = m[i - 1, j - 1] + getReplacementCost(a[i], b[j]);
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
                string buffer = String.Empty;
				while (i >= 0 || j >= 0)
				{
					int op = o[i, j];
					if (op == 1)
					{
						buffer = b[j] + buffer;
						j--;
					}
					else if (op == -1)
					{
						i--;
					}
					else
					{
						c[i] = b[j] + buffer;
                        buffer = String.Empty;
						i--;
						j--;
					}
				}
				if (buffer.Length > 0)
                {
                    c[0] = buffer + c[0];
                }
			}
			return c;
		}
	}
}

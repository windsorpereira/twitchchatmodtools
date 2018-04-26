using System;
using System.IO;
using System.Text;

namespace ModToolsImport
{
    class Program
    {
        static void Main(string[] args)
        {
            StringBuilder sb = new StringBuilder();

            DateTime data = DateTime.Now;

            foreach (string txtName in Directory.GetFiles(@"C:\Users\Windsor\.chatty\logs\logs_archive", "*.log"))
            {
                using (StreamReader sr = new StreamReader(txtName))
                {
                    string linha = "";

                    while((linha = sr.ReadLine()) != null)
                    {
                        if (linha.Contains("Log started"))
                        {
                            data = new DateTime(int.Parse(linha.Split(' ')[3].Split('-')[0]), int.Parse(linha.Split(' ')[3].Split('-')[1]), int.Parse(linha.Split(' ')[3].Split('-')[2]));
                        }
                        else
                        {
                            if (linha.Split(' ')[1][0] == '<')
                            {
                                var arrHora = linha.Split(']')[0].Replace("[", "").Split(':');
                                var horaMensagem = new TimeSpan(int.Parse(arrHora[0]), int.Parse(arrHora[2]), int.Parse(arrHora[2]));
                                var dataMensagem = data.Add(horaMensagem);

                                var usuario = linha.Split('>')[0].Split('<')[1];

                                bool mod = usuario.Contains("@") ? true : false;
                                bool sub = usuario.Contains("%") ? true : false;

                                usuario = usuario.Replace("@", "").Replace("%", "").Replace("^", "").Replace("+", "").Replace("$", "").Replace("~", "");

                                if (usuario.Split(' ').Length > 1)
                                {
                                    usuario = usuario.Split(' ')[1].Replace("(", "").Replace(")", "");
                                }

                                var mensagem = linha.Split('>')[1].Substring(1);
                            }
                            else if (linha.Contains("MOD_ACTION"))
                            {
                                var mod = linha.Split(' ')[2];

                                var usuario = linha.Split(' ')[4];

                                bool timeout = linha.Contains("(timeout") ? true : false;
                                bool ban = linha.Contains("(ban") ? true : false;

                                int tempoTimeout = 0;

                                if (linha.Contains("(timeout"))
                                {
                                    int.TryParse(linha.Split(' ')[5], out tempoTimeout);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

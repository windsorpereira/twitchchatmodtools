using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.SqlClient;

namespace ModToolsImport
{
    class Program
    {
        static void Main(string[] args)
        {
            using (SqlConnection conn = new SqlConnection(@"server=localhost\SQLExpress;Initial Catalog=modtools;Integrated Security=True"))
            {
                DateTime data = DateTime.Now;

                conn.Open();

                foreach (string txtName in Directory.GetFiles(@"C:\Users\Windsor\.chatty\logs\logs_archive", "*.log"))
                {
                    using (StreamReader sr = new StreamReader(txtName))
                    {
                        string linha = "";

                        while ((linha = sr.ReadLine()) != null)
                        {
                            if (linha.Contains("Log started"))
                            {
                                data = new DateTime(int.Parse(linha.Split(' ')[3].Split('-')[0]), int.Parse(linha.Split(' ')[3].Split('-')[1]), int.Parse(linha.Split(' ')[3].Split('-')[2]));
                            }
                            else
                            {
                                string usuario = "";
                                DateTime dataEvento = DateTime.Now;
                                bool mod = false;
                                bool sub = false;
                                string mensagem = "";

                                string viewer = "";

                                bool timeout = false;
                                bool ban = false;
                                int? tempoTimeout = 0;

                                bool modAction = false;
                                bool logChat = false;

                                if (linha.Split(' ').Length > 1 && linha.Split(' ')[1][0] == '<')
                                {
                                    logChat = true;

                                    var arrHora = linha.Split(']')[0].Replace("[", "").Split(':');
                                    var horaMensagem = new TimeSpan(int.Parse(arrHora[0]), int.Parse(arrHora[2]), int.Parse(arrHora[2]));

                                    dataEvento = data.Add(horaMensagem);

                                    usuario = linha.Split('>')[0].Split('<')[1];

                                    mod = usuario.Contains("@") ? true : false;
                                    sub = usuario.Contains("%") ? true : false;

                                    usuario = usuario.Replace("@", "").Replace("%", "").Replace("^", "").Replace("+", "").Replace("$", "").Replace("~", "");

                                    if (usuario.Split(' ').Length > 1)
                                    {
                                        usuario = usuario.Split(' ')[1].Replace("(", "").Replace(")", "");
                                    }

                                    mensagem = linha.Split('>')[1].Substring(1);
                                }
                                else if (linha.Contains("MOD_ACTION"))
                                {   
                                    var arrHora = linha.Split(']')[0].Replace("[", "").Split(':');
                                    var horaMensagem = new TimeSpan(int.Parse(arrHora[0]), int.Parse(arrHora[2]), int.Parse(arrHora[2]));
                                    dataEvento = data.Add(horaMensagem);

                                    usuario = linha.Split(' ')[2];
                                    
                                    timeout = linha.Contains("(timeout") ? true : false;
                                    ban = linha.Contains("(ban") ? true : false;
                                    
                                    if (linha.Contains("(timeout"))
                                    {
                                        tempoTimeout = int.Parse(linha.Split(' ')[5].ToString());
                                    }

                                    if (ban || timeout)
                                    {
                                        modAction = true;
                                        viewer = linha.Split(' ')[4];
                                    }
                                }

                                if (logChat || modAction)
                                {
                                    var sqlUsuario = "select * from usuario where st_login = '{0}'";

                                    var sqlInsertedId = "SELECT SCOPE_IDENTITY()";

                                    var sqlInsertUsuario = "insert into usuario (st_login, fl_mod, fl_sub) values ('{0}', {1}, {2})";
                                    var sqlUpdateUsuario = "update usuario set fl_mod = {1}, fl_sub = {2} where st_login = '{0}'";

                                    var queryUsuario = new SqlCommand(string.Format(sqlUsuario, usuario), conn);

                                    var dbUsuario = queryUsuario.ExecuteScalar();

                                    int idUsuario = 0;
                                    int idViewer = 0;
                                    
                                    if (dbUsuario == null)
                                    {
                                        var queryInsertUsuario = new SqlCommand(string.Format(sqlInsertUsuario, usuario, (mod ? 1 : 0), (sub ? 1 : 0)), conn);

                                        queryInsertUsuario.ExecuteNonQuery();

                                        var queryId = new SqlCommand(sqlInsertedId, conn);

                                        idUsuario = int.Parse(queryId.ExecuteScalar().ToString());
                                    }
                                    else
                                    {
                                        if (!modAction)
                                        {
                                            var queryUpdateUsuario = new SqlCommand(string.Format(sqlUpdateUsuario, usuario, (mod ? 1 : 0), (sub ? 1 : 0)), conn);

                                            queryUpdateUsuario.ExecuteNonQuery();
                                        }

                                        idUsuario = int.Parse(dbUsuario.ToString());
                                    }

                                    if (modAction)
                                    {
                                        queryUsuario = new SqlCommand(string.Format(sqlUsuario, viewer), conn);

                                        dbUsuario = queryUsuario.ExecuteScalar();

                                        if (dbUsuario == null)
                                        {
                                            var queryInsertUsuario = new SqlCommand(string.Format(sqlInsertUsuario, viewer, 0, 0), conn);

                                            queryInsertUsuario.ExecuteNonQuery();

                                            var queryId = new SqlCommand(sqlInsertedId, conn);

                                            idViewer = int.Parse(queryId.ExecuteScalar().ToString());
                                        }
                                        else
                                        {
                                            idViewer = int.Parse(dbUsuario.ToString());
                                        }

                                        var sqlInsertModAction = "insert into moderacao (id_moderador, id_viewer, dt_data, fl_ban, fl_timeout, in_tempo) values (@idModerador, @idViewer, @data, @ban, @timeout, @tempo)";

                                        var sqlModAction = "select count(1) from moderacao where id_moderador = @idModerador and dt_data = @data";

                                        var queryModAction = new SqlCommand(sqlModAction, conn);

                                        queryModAction.Parameters.Add(new SqlParameter("data", dataEvento));
                                        queryModAction.Parameters.Add(new SqlParameter("idModerador", idUsuario));

                                        if (int.Parse(queryModAction.ExecuteScalar().ToString()) <= 0)
                                        {
                                            var queryInsertModAction = new SqlCommand(sqlInsertModAction, conn);

                                            queryInsertModAction.Parameters.Add(new SqlParameter("data", dataEvento));
                                            queryInsertModAction.Parameters.Add(new SqlParameter("idModerador", idUsuario));
                                            queryInsertModAction.Parameters.Add(new SqlParameter("idViewer", idViewer));
                                            queryInsertModAction.Parameters.Add(new SqlParameter("ban", ban));
                                            queryInsertModAction.Parameters.Add(new SqlParameter("timeout", timeout));
                                            queryInsertModAction.Parameters.Add(new SqlParameter("tempo", tempoTimeout));

                                            queryInsertModAction.ExecuteNonQuery();
                                        }
                                    }
                                    else if (logChat)
                                    {
                                        var sqlInsertMensagem = "insert into mensagem (id_usuario, dt_data, st_mensagem) values (@idUsuario, @data, @mensagem)";

                                        var sqlMensagem = "select count(1) from mensagem where dt_data = @data AND id_usuario = @idUsuario";

                                        var queryMensagem = new SqlCommand(sqlMensagem, conn);

                                        queryMensagem.Parameters.Add(new SqlParameter("data", dataEvento));
                                        queryMensagem.Parameters.Add(new SqlParameter("idUsuario", idUsuario));

                                        if (int.Parse(queryMensagem.ExecuteScalar().ToString()) <= 0)
                                        {
                                            var queryInsertMensagem = new SqlCommand(sqlInsertMensagem, conn);

                                            queryInsertMensagem.Parameters.Add(new SqlParameter("data", dataEvento));
                                            queryInsertMensagem.Parameters.Add(new SqlParameter("idUsuario", idUsuario));
                                            queryInsertMensagem.Parameters.Add(new SqlParameter("mensagem", mensagem));

                                            queryInsertMensagem.ExecuteNonQuery();
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
}

using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace exemplo_comunicacao_intelbras
{
    public class IntelbrasComunicacao
    {
        const string GET_FUNCIONARIO_POR_IDENTIFICADOR = "/cgi-bin/AccessUser.cgi?action=list&UserIDList[0]=";
        const string GET_REGISTROS_POR_DATA = "/cgi-bin/recordFinder.cgi?action=find&name=AccessControlCardRec&StartTime=";
        const string SET_DATA_HORA_DISPOSITIVO = "/cgi-bin/global.cgi?action=setCurrentTime&time=";
        const string CADASTRAR_PESSOAS = "/cgi-bin/AccessUser.cgi?action=insertMulti"; // o comando é usado para cadastrar uma ou mais pessoas.
        const string CADASTRAR_FACES = "/cgi-bin/AccessFace.cgi?action=insertMulti"; // o comando é usado para cadatrar uma ou mais faces.

        public required IntelbrasConfiguracao mIntelbrasConfiguracao {  get; set; }

        public async Task<Funcionario> ReceberDadosPessoaPorIdAsync(int id)
        {
            var url = $"http://{mIntelbrasConfiguracao.Ip}:{mIntelbrasConfiguracao.Porta}{GET_FUNCIONARIO_POR_IDENTIFICADOR}{id}";

            var content = await FazerRequisicaoAsync(url);

            var lines = content.Split("\r\n");

            var funcionario = new Funcionario();

            foreach (var line in lines)
            {
                // Linha divida pelo sinal de igual "=" para obter a chave e o valor
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    var key = parts[0];
                    var value = parts[1];

                    switch (key)
                    {
                        case "Users[0].UserID":
                            funcionario.Identificador = value ;
                            break;
                        case "Users[0].UserName":
                            funcionario.Nome = value;
                            break;
                        case "Users[0].Password":
                            funcionario.Senha = value;
                            break;
                    }
                }
            }

            return funcionario;
        }

        public async Task<string> EnviarFaces()
        {
            var url = $"http://{mIntelbrasConfiguracao.Ip}:{mIntelbrasConfiguracao.Porta}{CADASTRAR_FACES}";

            // Resolução mínima: 150 × 300 pixels(L × A)
            // Resolução Máxima: 600 × 1200 pixels(L × A)
            // A altura não deve exceder duas vezes a largura*
            // Tamanho máximo de arquivo: 100KB
            var imageBytes = File.ReadAllBytes("gabriel_foto_comprimida.jpg");

            var fotoBase64 = Convert.ToBase64String(imageBytes);

            var facesParaEnvio = new
            {
                FaceList = new List<DadosEnvioFace>()
                {
                    new()
                    {
                        UserID = "16",
                        PhotoData = new List<string> { fotoBase64 }
                    }
                }
            };

            var content = await FazerRequisicaoAsync(url, JsonConvert.SerializeObject(facesParaEnvio));

            var lines = content.Split("\r\n");

            return lines[0].ToString();
        }

        public async Task<string> EnviarDataHoraAsync()
        {
            var dataHora = DateTime.Now;
            
            var dataHoraFormatado = dataHora.ToString("yyyy-MM-dd'%20'HH:mm:ss");

            var url = $"http://{mIntelbrasConfiguracao.Ip}:{mIntelbrasConfiguracao.Porta}{SET_DATA_HORA_DISPOSITIVO}{dataHoraFormatado}";

            var content = await FazerRequisicaoAsync(url);

            var lines = content.Split("\r\n");

            return lines[0].ToString();
        }

        public async Task<string> EnviarFuncionariosAsync()
        {
            var url = $"http://{mIntelbrasConfiguracao.Ip}:{mIntelbrasConfiguracao.Porta}{CADASTRAR_PESSOAS}";

            // Para enviar mais funcionários basta adicionar na lista UserList
            var listaFuncionariosEnvio = new
            {
                UserList = new List<Funcionario>
                {
                    new() {
                        Identificador = "5",
                        Nome = "JOAO GALINHA",
                        UserType = 0,
                        Authority = 1,
                        Senha = "123",
                        Doors = new List<int> { 0 },
                        TimeSections = new List<int> { 255 },
                        ValidFrom = DateTime.Parse("2019-01-02 00:00:00"),
                        ValidTo = DateTime.Parse("2037-01-02 01:00:00")
                    }
                }
            };

        var content = await FazerRequisicaoAsync(url, JsonConvert.SerializeObject(listaFuncionariosEnvio));

            var lines = content.Split("\r\n");

            return lines[0].ToString();
        }

        public async Task<List<RegistrosOffline>> ReceberUltimosRegistrosAsync()
        {
            var url = $"http://{mIntelbrasConfiguracao.Ip}{GET_REGISTROS_POR_DATA}{mIntelbrasConfiguracao.UltimoRegistroLido}";

            var content = await FazerRequisicaoAsync(url);

            var lines = content.Split("\r\n");

            var listaRegistro = new List<RegistrosOffline>();

            var quantidadeRegistros = int.Parse(lines[0]["found=".Length..]); 

            for (int i = 0; i < quantidadeRegistros; i++)
            {
                var dadosRegistro = lines.Where(line => line.StartsWith($"records[{i}]")).ToList();

                var registro = new RegistrosOffline();
                foreach (var dados in dadosRegistro)
                {
                    var keyWithoutIndex = dados.Substring($"records[{i}]".Length + 1); // Adicionamos 1 para também remover o ponto

                    // Divide a linha em chave e valor com base no sinal de igual "="
                    var parts = keyWithoutIndex.Split('=');
                    var key = parts[0];
                    var value = parts[1];

                    switch (key)
                    {
                        case "UserID":
                            registro.IdentificadorFuncionario = value;
                            break;
                        case "CardName":
                            registro.NomeFuncionario = value;
                            break;
                        case "CreateTime":
                            var timeSeconds = long.Parse(value); 

                            // O equipamento sempre retorna a data e hora com o timezone 0, então teremos que configurar
                            // manualmente em qual timezone será salvo o registro.
                            registro.dataHoraRegistro = DateTimeOffset.FromUnixTimeSeconds(timeSeconds).LocalDateTime;
                            break;
                    }
                }

                listaRegistro.Add(registro);
            }

            return listaRegistro;
        }

        private async Task<string> FazerRequisicaoAsync(string url, string conteudo = null)
        {
            WebRequest request = WebRequest.CreateHttp(url);

            request.Credentials = new CredentialCache()
            {
                { request.RequestUri, "Digest", new NetworkCredential(mIntelbrasConfiguracao.Usuario, mIntelbrasConfiguracao.Senha) }
            };

            if (conteudo != null)
            {
                request.Method = "POST";
                request.ContentType = "application/json";

                using (var requestStream = new StreamWriter(request.GetRequestStream()))
                {
                    await requestStream.WriteAsync(conteudo);
                }
            }

            WebResponse response = await request.GetResponseAsync();

            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                string content = await streamReader.ReadToEndAsync();

                return content;
            }
        }

        private async Task<string> FazerRequisicaoAsync1(string url, string? conteudo = null)
        {
            var handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential(mIntelbrasConfiguracao.Usuario, mIntelbrasConfiguracao.Senha)
            };

            using var httpClient = new HttpClient(handler);

            HttpResponseMessage response;
            if (conteudo == null)
            {
                response = await httpClient.GetAsync(url);
            }
            else
            {
                var conteudoHttp = new StringContent(conteudo, Encoding.UTF8, "application/json");
                response = await httpClient.PostAsync(url, conteudoHttp);
            }

            return await response.Content.ReadAsStringAsync();
        }

        public class Funcionario
        {
            [JsonProperty("UserID")]
            public string Identificador { get; set; }
            [JsonProperty("UserName")]
            public string Nome { get; set; }
            [JsonProperty("Password")]
            public string Senha { get; set; }
            public int UserType { get; set; }
            public int Authority { get; set; }
            public List<int> Doors { get; set; }
            public List<int> TimeSections { get; set; }
            public DateTime ValidFrom { get; set; }
            public DateTime ValidTo { get; set; }
        }

        public class RegistrosOffline
        {
            public string IdentificadorFuncionario { get; set; }
            public string NomeFuncionario { get;set; }
            public DateTime dataHoraRegistro {  get; set; }

        }

        public class DadosEnvioFace
        {
            public string UserID { get; set; }
            public List<string> PhotoData { get; set; }
        }
    }
}

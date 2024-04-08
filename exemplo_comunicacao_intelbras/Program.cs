using exemplo_comunicacao_intelbras;

class Program
{
    static async Task Main()
    {
        var ultimoRegistroUnix = new DateTimeOffset(DateTime.Now.AddDays(-1)).ToUnixTimeSeconds();

        var configuracoesIntelbras = new IntelbrasConfiguracao()
        {
            Usuario = "username",
            Senha = "password",
            Ip = "192.168.1.x",
            Porta = 80,
            UltimoRegistroLido = ultimoRegistroUnix
        };

        var comunicacaoIntelbras = new IntelbrasComunicacao()
        {
            mIntelbrasConfiguracao = configuracoesIntelbras
        };

        Console.WriteLine("Dados funcionario");
        var dadosPessoa = await comunicacaoIntelbras.ReceberDadosPessoaPorIdAsync(5);

        Console.WriteLine($"Identificador: {dadosPessoa.Identificador}");
        Console.WriteLine($"Nome: {dadosPessoa.Nome}");
        Console.WriteLine($"Senha: {dadosPessoa.Senha}");

        Console.WriteLine();

        Console.WriteLine("Envio data e hora");
        var statusEnvioDataHora = await comunicacaoIntelbras.EnviarDataHoraAsync();

        Console.WriteLine($"Envio: {statusEnvioDataHora}");
        Console.WriteLine();

        Console.WriteLine("Envio funcionário");
        var statusCadastroFuncionario = await comunicacaoIntelbras.EnviarFuncionariosAsync();

        Console.Write($"Envio: {statusCadastroFuncionario}");

        Console.WriteLine("Registros realizados");
        var registrosOffline = await comunicacaoIntelbras.ReceberUltimosRegistrosAsync();
        Console.WriteLine();

        foreach (var registro in registrosOffline)
        {
            Console.WriteLine($"Funcionario: {registro.NomeFuncionario}");
            Console.WriteLine($"Identificador: {registro.IdentificadorFuncionario}");
            Console.WriteLine($"Data e hora registro: {registro.dataHoraRegistro}");

            Console.WriteLine();
        }

        Console.WriteLine("Envio de face");
        var envioFace = await comunicacaoIntelbras.EnviarFaces();

        Console.WriteLine(envioFace);

        Console.ReadLine();
    }
}

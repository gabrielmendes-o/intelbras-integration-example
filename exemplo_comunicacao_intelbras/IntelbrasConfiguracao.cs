namespace exemplo_comunicacao_intelbras
{
    public class IntelbrasConfiguracao
    {
        public required string Usuario { get; set; }
        public required string Senha { get; set; }
        public required string Ip { get; set; }
        public required ushort Porta { get; set; } = 80;
        public long UltimoRegistroLido { get; set; }
    }
}

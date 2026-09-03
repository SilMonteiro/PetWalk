public class Pet
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Raca { get; set; } = "";
    public string Especie { get; set; } = "";
    public string Porte { get; set; } = "";
    public int Idade { get; set; }
    public double Avaliacao { get; set; }
    public string Animal { get; set; }
    public bool Castrado { get; set; }
    public List<string> Vacinas { get; set; } = new List<string>();
    public string Localizacao { get; set; } = "";

    public int TutorId { get; set; }
}
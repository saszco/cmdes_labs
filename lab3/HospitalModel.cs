namespace Lab3;

// Завдання 3. Приймальне відділення лікарні з трьома типами хворих (час у хвилинах)
public class HospitalModel
{
    public Create Patients { get; }
    public Process Reception { get; }     // два чергових лікарі
    public Process ToWard { get; }        // троє супровідних, слідування в палату
    public Process ToLab { get; }         // слідування в лабораторію
    public Process LabRegistry { get; }   // реєстратура лабораторії
    public Process Lab { get; }           // два лаборанти
    public Process ToReception { get; }   // повернення з лабораторії
    public Dispose Ward { get; }          // хворий доставлений у палату
    public Dispose LeftLab { get; }       // хворий залишив лікарню після аналізів
    public Model Model { get; }

    private readonly Process[] all;

    public HospitalModel()
    {
        // тип хворого визначається при надходженні за відносними частотами 0.5, 0.1, 0.4
        Patients = new Create("PATIENTS", () => FunRand.Exp(15), NewPatient);

        // середній час реєстрації залежить від типу: 15, 40, 30 хв
        Reception = new Process("RECEPTION", item => FunRand.Exp(item.Type switch { 1 => 15, 2 => 40, _ => 30 }), 2)
        {
            // лікар, який звільнився, першим бере хворого, що пройшов обстеження (тип 1)
            QueueRule = q => Math.Max(0, q.FindIndex(i => i.Type == 1)),
            ShowTypes = true,
        };
        ToWard = new Process("TO_WARD", () => FunRand.Unif(3, 8), 3) { ShowTypes = true };
        ToLab = new Process("TO_LAB", () => FunRand.Unif(2, 5), channels: 0) { ShowTypes = true };
        LabRegistry = new Process("LAB_REGISTRY", () => FunRand.Erlang(4.5, 3)) { ShowTypes = true };
        Lab = new Process("LAB", () => FunRand.Erlang(4, 2), 2) { ShowTypes = true };
        ToReception = new Process("TO_RECEPTION", () => FunRand.Unif(2, 5), channels: 0) { ShowTypes = true };
        Ward = new Dispose("WARD");
        LeftLab = new Dispose("LEFT_LAB");

        Patients.AddRoute(Reception);
        // маршрути після приймального відділення та лабораторії залежать від типу хворого
        Reception.AddRoute(ToWard, accepts: i => i.Type == 1);
        Reception.AddRoute(ToLab, accepts: i => i.Type != 1);
        ToWard.AddRoute(Ward);
        ToLab.AddRoute(LabRegistry);
        LabRegistry.AddRoute(Lab);
        Lab.AddRoute(ToReception, accepts: i => i.Type == 2);
        Lab.AddRoute(LeftLab, accepts: i => i.Type == 3);
        // після повернення в приймальне відділення хворий розглядається як хворий типу 1
        ToReception.AddRoute(Reception, onPass: i => i.Type = 1);

        all = new[] { Reception, ToWard, ToLab, LabRegistry, Lab, ToReception };
        Model = new Model(Patients, Reception, ToWard, ToLab, LabRegistry, Lab, ToReception, Ward, LeftLab);
    }

    private static Item NewPatient()
    {
        double r = FunRand.Uniform01();
        return new Item(r <= 0.5 ? 1 : r <= 0.6 ? 2 : 3);
    }

    // Середній час у системі для хворих типу k: сумарний час перебування хворих
    // цього типу в усіх чергах і каналах, поділений на кількість хворих, що
    // завершили процес (тип 1 і 2 — доставлені в палату, тип 3 — вийшли з лабораторії)
    public double MeanTimeInSystem(int type)
    {
        int finished = type == 3 ? LeftLab.CountOfType(3) : Ward.CountOfType(type);
        return finished == 0 ? 0 : all.Sum(p => p.TimeOfType(type)) / finished;
    }

    public void PrintResult()
    {
        double t = Model.Tcurr;
        Console.WriteLine($"Час моделювання: {t} хв");
        Console.WriteLine($"Хворих надійшло: {Patients.Quantity}, доставлено в палату: {Ward.Quantity} " +
                          $"(тип 1: {Ward.CountOfType(1)}, тип 2: {Ward.CountOfType(2)}), " +
                          $"залишили лікарню після лабораторії: {LeftLab.Quantity}");
        for (int k = 1; k <= 3; k++)
            Console.WriteLine($"Середній час у системі, тип {k}       = {MeanTimeInSystem(k):F2} хв");
        Console.WriteLine($"Середній інтервал між прибуттями в лабораторію = {LabRegistry.MeanArrivalInterval:F2} хв");
        Console.WriteLine();
        Console.WriteLine($"{"СМО",-14} {"канали",6} {"завант.",8} {"Lq",7} {"очік.",7} {"обслуг.",8}");
        foreach (var p in all)
        {
            string ch = p.Channels < 0 ? "∞" : p.Channels.ToString();
            string load = p.Channels < 0 ? $"{p.MeanBusy(t),8:F3}*" : $"{p.MeanLoad(t),8:F3}";
            Console.WriteLine($"{p.Name,-14} {ch,6} {load} {p.MeanQueue(t),7:F3} {p.MeanWait(),7:F2} {p.Quantity,8}");
        }
        Console.WriteLine("* для СМО з необмеженою кількістю каналів — середня кількість зайнятих каналів");
    }
}

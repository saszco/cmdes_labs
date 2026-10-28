namespace Lab3;

// Завдання 2. Банк для автомобілістів: два віконця, у кожного касира своя смуга
public class BankModel
{
    public const int MaxQueue = 3;   // на смузі не більше трьох автомобілів у черзі

    public Create Clients { get; }
    public Process Cashier1 { get; }
    public Process Cashier2 { get; }
    public Dispose Exit { get; }
    public Model Model { get; }
    public int LaneChanges { get; private set; }

    public BankModel()
    {
        // інтервали між прибуттями exp(0.5), перше прибуття в момент 0.1
        Clients = new Create("CLIENTS", () => FunRand.Exp(0.5), firstArrival: 0.1);
        Cashier1 = new Process("CASHIER1", () => FunRand.Exp(0.3)) { MaxQueue = MaxQueue };
        Cashier2 = new Process("CASHIER2", () => FunRand.Exp(0.3)) { MaxQueue = MaxQueue };
        Exit = new Dispose("EXIT");

        // Вибір смуги за пріоритетом: перша смуга має вищий пріоритет, але маршрут
        // до неї блокується, якщо на першій смузі автомобілів більше, ніж на другій.
        // Тоді при рівних чергах (і за їх відсутності) обирається перша смуга,
        // в інших випадках — коротша.
        Clients.AddRoute(Cashier1, priority: 2, block: () => Cashier1.Count > Cashier2.Count);
        Clients.AddRoute(Cashier2, priority: 1);
        Cashier1.AddRoute(Exit);
        Cashier2.AddRoute(Exit);

        // Початкові умови: обидва касири зайняті, тривалість цього обслуговування N(1, 0.3);
        // у кожній черзі очікують по два автомобілі
        foreach (var cashier in new[] { Cashier1, Cashier2 })
        {
            cashier.PreloadChannel(new Item(), Math.Max(0, FunRand.Norm(1.0, 0.3)));
            cashier.PreloadQueue(new Item());
            cashier.PreloadQueue(new Item());
        }

        Model = new Model(Clients, Cashier1, Cashier2, Exit);
        Model.AfterEvent.Add(ChangeLane);
    }

    // Клієнт, що стоїть останнім, змінює чергу, якщо різниця в кількості
    // автомобілів на смугах не менше двох
    private string ChangeLane()
    {
        foreach (var (from, to) in new[] { (Cashier1, Cashier2), (Cashier2, Cashier1) })
        {
            if (from.Queue > 0 && from.Count - to.Count >= 2)
            {
                to.InAct(from.TakeLastFromQueue());
                LaneChanges++;
                return $"зміна смуги: {from.Name} -> {to.Name}";
            }
        }
        return null;
    }

    public int Refused => Cashier1.Failure + Cashier2.Failure;

    // Загальний час перебування клієнтів у банку: в чергах і в каналах обслуговування
    public double TotalTimeInBank =>
        Cashier1.QueueTime + Cashier1.BusyTime + Cashier2.QueueTime + Cashier2.BusyTime;

    public void PrintResult()
    {
        double t = Model.Tcurr;
        Console.WriteLine($"Час моделювання: {t}");
        Console.WriteLine($"Клієнтів прибуло: {Clients.Quantity}, обслуговано: {Exit.Quantity}, відмовлено: {Refused}");
        Console.WriteLine($"1) середнє завантаження касира 1      = {Cashier1.MeanLoad(t):F4}");
        Console.WriteLine($"   середнє завантаження касира 2      = {Cashier2.MeanLoad(t):F4}");
        Console.WriteLine($"2) середнє число клієнтів у банку     = {TotalTimeInBank / t:F4}");
        Console.WriteLine($"3) середній інтервал між від'їздами   = {Exit.MeanArrivalInterval:F4}");
        Console.WriteLine($"4) середній час перебування в банку   = {TotalTimeInBank / Exit.Quantity:F4}");
        Console.WriteLine($"5) середнє число клієнтів у черзі 1   = {Cashier1.MeanQueue(t):F4}");
        Console.WriteLine($"   середнє число клієнтів у черзі 2   = {Cashier2.MeanQueue(t):F4}");
        Console.WriteLine($"6) відсоток відмов                    = {100.0 * Refused / Clients.Quantity:F2}%");
        Console.WriteLine($"7) число змін під'їзних смуг          = {LaneChanges}");
    }
}

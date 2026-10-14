namespace Lab2;

// Пристрій обслуговування з чергою; може мати кілька ідентичних каналів
public class Process : Element
{
    public int Queue { get; private set; }
    public int MaxQueue { get; set; } = int.MaxValue; // обмеження довжини черги
    public int Failure { get; private set; }          // кількість відмов
    public int Channels => channelTnext.Length;        // кількість пристроїв

    private readonly double[] channelTnext; // момент звільнення кожного каналу (MaxValue — вільний)

    private double queueSum; // ∑ L_i·Δt_i — для середньої довжини черги
    private double busySum;  // ∑ (зайняті канали)·Δt_i — для середнього завантаження

    public Process(string name, double delay, int channels = 1) : base(name, delay)
    {
        channelTnext = Enumerable.Repeat(double.MaxValue, channels).ToArray();
    }

    public int BusyChannels => channelTnext.Count(t => t != double.MaxValue);

    public override void InAct()
    {
        // Канали перебираються по черзі, доки не знайдеться вільний
        int free = Array.IndexOf(channelTnext, double.MaxValue);
        if (free >= 0)
            channelTnext[free] = Tcurr + GetDelay();
        else if (Queue < MaxQueue)
            Queue++;
        else
            Failure++;

        UpdateTnext();
    }

    public override void OutAct()
    {
        for (int i = 0; i < channelTnext.Length; i++)
        {
            if (channelTnext[i] != Tcurr) continue;

            Quantity++;
            channelTnext[i] = double.MaxValue;

            // Спочатку беремо наступну вимогу з черги...
            if (Queue > 0)
            {
                Queue--;
                channelTnext[i] = Tcurr + GetDelay();
            }

            // ...потім відправляємо обслужену вимогу далі (можливо, назад у цей же Process)
            ChooseNext()?.InAct();
        }

        UpdateTnext();
    }

    // Tnext процесу = найближче звільнення серед усіх каналів
    private void UpdateTnext() => Tnext = channelTnext.Min();

    public override void DoStatistics(double delta)
    {
        queueSum += Queue * delta;
        busySum += BusyChannels * delta;
    }

    public double MeanQueue(double totalTime) => queueSum / totalTime;
    public double MeanBusy(double totalTime) => busySum / totalTime;              // середня к-сть зайнятих каналів
    public double MeanLoad(double totalTime) => busySum / totalTime / Channels;   // завантаження одного пристрою (0..1)
    public double MeanWait() => Quantity == 0 ? 0 : queueSum / Quantity;           // середній час очікування в черзі
    public double FailureProbability() =>
        Quantity + Failure == 0 ? 0 : Failure / (double)(Quantity + Failure);

    public override void PrintInfo()
    {
        string states = string.Join("", channelTnext.Select(t => t == double.MaxValue ? "0" : "1"));
        Console.WriteLine($"{Name} state = {states} queue = {Queue} quantity = {Quantity} " +
                          $"failure = {Failure} tnext = {FormatTime(Tnext)}");
    }

    public void PrintResult(double totalTime)
    {
        PrintResult();
        Console.WriteLine($"  mean length of queue = {MeanQueue(totalTime):F4}");
        Console.WriteLine($"  mean load            = {MeanLoad(totalTime):F4}" +
                          (Channels > 1 ? $" (mean busy channels = {MeanBusy(totalTime):F4} of {Channels})" : ""));
        Console.WriteLine($"  mean waiting time    = {MeanWait():F4}");
        Console.WriteLine($"  failure              = {Failure}");
        Console.WriteLine($"  failure probability  = {FailureProbability():F4}");
    }
}

namespace Lab3;

// Надходження вимог ззовні з заданою часовою затримкою
public class Create : Element
{
    private readonly Func<double> delay;
    private readonly Func<Item> newItem;   // визначає тип вимоги при надходженні

    public Create(string name, Func<double> delay, Func<Item> newItem = null, double firstArrival = 0.0)
        : base(name)
    {
        this.delay = delay;
        this.newItem = newItem ?? (() => new Item());
        Tnext = firstArrival; // момент першого надходження
    }

    public override void InAct(Item item) =>
        throw new InvalidOperationException("Create не приймає вимог");

    public override void OutAct()
    {
        Quantity++;
        Tnext = Tcurr + delay();
        if (!Pass(newItem())) RouteFailures++;
    }
}

namespace Lab2;

// Генератор вимог
public class Create : Element
{
    public Create(string name, double delay) : base(name, delay)
    {
        Tnext = 0.0; // імітація починається з події Create
    }

    public override void OutAct()
    {
        base.OutAct();
        Tnext = Tcurr + GetDelay();  // момент надходження наступної вимоги
        ChooseNext()?.InAct();       // передаємо вимогу в nextElement
    }
}

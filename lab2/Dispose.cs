namespace Lab2;

// Вихід з моделі: лише рахує вимоги, що завершили обслуговування
public class Dispose : Element
{
    public Dispose(string name) : base(name, 0) { }

    public override void InAct() => Quantity++;
}

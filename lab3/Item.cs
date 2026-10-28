namespace Lab3;

// Вимога (замовлення), що проходить мережу МО.
// Type — поточний тип (визначає параметри обслуговування і маршрут),
// InitialType — тип при надходженні в мережу (для статистики за типами).
public class Item
{
    public int Type { get; set; }
    public int InitialType { get; }

    public Item(int type = 1)
    {
        Type = type;
        InitialType = type;
    }
}

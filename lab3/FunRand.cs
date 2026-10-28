namespace Lab3;

// Генератори випадкових величин
public static class FunRand
{
    private static Random rnd = new Random(1);

    // Seed для відтворюваних експериментів
    public static void SetSeed(int seed) => rnd = new Random(seed);

    public static double Uniform01()
    {
        double a = 0;
        while (a == 0) a = rnd.NextDouble();
        return a;
    }

    // Експоненційний розподіл із середнім timeMean
    public static double Exp(double timeMean) => -timeMean * Math.Log(Uniform01());

    // Рівномірний розподіл на [timeMin, timeMax]
    public static double Unif(double timeMin, double timeMax) => timeMin + Uniform01() * (timeMax - timeMin);

    // Нормальний розподіл (метод Бокса-Мюллера)
    public static double Norm(double timeMean, double timeDeviation)
    {
        double z = Math.Sqrt(-2 * Math.Log(Uniform01())) * Math.Cos(2 * Math.PI * Uniform01());
        return timeMean + timeDeviation * z;
    }

    // Закон Ерланга з математичним сподіванням timeMean і параметром k:
    // сума k експоненційних величин із середнім timeMean / k
    public static double Erlang(double timeMean, int k)
    {
        double sum = 0;
        for (int i = 0; i < k; i++)
            sum += Exp(timeMean / k);
        return sum;
    }
}

namespace Lab1.DataLayer;

public static class TaskHelper
{
    public static async void FireAndForget(this Task t)
    {
        try
        {
            await t;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

}
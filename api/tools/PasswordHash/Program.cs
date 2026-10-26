using Microsoft.AspNetCore.Identity;
if (Console.IsInputRedirected)
{
    var secret=Console.ReadLine();
    if(string.IsNullOrEmpty(secret)) throw new InvalidOperationException("Senha vazia.");
    Console.WriteLine(new PasswordHasher<string>().HashPassword("account",secret));
}
else
{
    Console.Error.Write("Senha (entrada oculta): ");
    var value=new System.Text.StringBuilder();
    ConsoleKeyInfo key;
    while((key=Console.ReadKey(true)).Key!=ConsoleKey.Enter) {
        if(key.Key==ConsoleKey.Backspace) { if(value.Length>0)value.Length--; }
        else if(!char.IsControl(key.KeyChar))value.Append(key.KeyChar);
    }
    Console.Error.WriteLine();
    if(value.Length==0)throw new InvalidOperationException("Senha vazia.");
    Console.WriteLine(new PasswordHasher<string>().HashPassword("account",value.ToString()));
}

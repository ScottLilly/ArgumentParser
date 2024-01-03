# ArgumentParser

Parses a string, or list of strings, into a ParsedArguments object that is easier to use.

This was originally written to parse chat messages from Twitch chat.
I wrote a Twitch bot that played a game where chatters managed a business [MegaCorpClash](https://github.com/ScottLilly/MegaCorpClash).
One thing they could do was hire employees.
For example, to hire five managers, they would type:
```
!hire manager 5"
```

I wanted a way to search for words in the chat message that matched a value from the EmployeeType enum and find the integer value from the chat message, to determine how many managers to hire:
```
public enum EmployeeType
{
    Production,
    Sales,
    Marketing,
    Research,
    HR,
    Legal,
    Security,
    IT,
    PR,
    Spy
}
```

## How to use

So, you could write code like this:
```
var chatMessage = "sales 1"; // This would be the message from Twitch chat

var parser = new Parser();
var parsedArguments = parser.Parse(chatMessage);

// "hire" message expects an employee type and number of employees
var employeeType = parsedArguments.GetEnum<EmployeeType>(0);
var numberOfEmployees = parsedArguments.GetInt(0);

// Do hiring logic here
```

That's how this project started.

Now, I use it in other projects, to parse command-line arguments, and other strings.
Please let me know if you find any issues, have suggestions, or have an idea for another feature.

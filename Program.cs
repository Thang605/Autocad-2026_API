using System;
using System.Reflection;
using Autodesk.Civil.DatabaseServices.Styles;
class Program {
    static void Main() {
        foreach (var name in Enum.GetNames(typeof(StaggerLabelType))) {
            Console.WriteLine("FOUND: " + name);
        }
    }
}

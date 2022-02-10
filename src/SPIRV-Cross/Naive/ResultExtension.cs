using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SPIRVCross.Naive
{
    internal static class ResultExtension
    {
        private static Dictionary<Result, SpvcException> _managedSpvcResultExceptions;
        public static IEnumerable<KeyValuePair<Result, SpvcException>> ManagedExceptions => _managedSpvcResultExceptions.AsEnumerable();

        static ResultExtension()
        {
            _managedSpvcResultExceptions = new Dictionary<Result, SpvcException>(Enum.GetValues<Result>().Length);
            RecordAllSupportedExceptions();
        }
        
        public static bool IsError(this Result result) => result != Result.Success;
        internal static Result AsManaged(this Base.spvc_result result) => (Result) result;
        public static void ThrowIfError(this Result result, string message)
        {
            if (result.IsError()) throw (Activator.CreateInstance(_managedSpvcResultExceptions[result].GetType(), message, result) as SpvcException)!;
        }
        
        private static void RecordAllSupportedExceptions()
        {
            // same logic as in Carbon.Exceptions.ResultExtension
            
            System.Type formatExceptionType = typeof(SpvcException);
            System.Type attributeType = typeof(ResultAttribute);
            
            foreach (System.Type type in Assembly.GetExecutingAssembly().GetTypes())
            {  
                if (type.IsAssignableTo(formatExceptionType))
                {  
                    object[] attributes = type.GetCustomAttributes(attributeType, false);
                    for (i32 i = 0; i < attributes.Length; i++)
                    {
                        if (attributes[i] is ResultAttribute attribute)
                        {
                            Result result = attribute.Result;
                            if (!_managedSpvcResultExceptions.TryAdd(result, (SpvcException)Activator.CreateInstance(type)!)) // no duplicate !
                                throw new Exception($"Result {result} already defined in {_managedSpvcResultExceptions[result].GetType().Name}: {type.Name} is a duplicate.");
                            break; // don't care about any other possible attributes.
                        }
                    }
                }
            }
        }
    }
}
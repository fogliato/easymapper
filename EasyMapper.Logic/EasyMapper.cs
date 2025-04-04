using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace EasyMapper.Logic
{

    public static class Mapper
    {
        /// <summary>
        /// Method that performs property binding.
        /// </summary>
        /// <typeparam name="T">Object that will be populated.</typeparam>
        /// <typeparam name="U">Type of object to be copied.</typeparam>
        /// <param name="obj">Instance of the object to be copied.</param>
        /// <param name="excludes">List of Properties that will be excluded from the Bind.</param>
        /// <returns>Populated object.</returns>
        [DebuggerStepThrough]
        public static T PopulateProperties<T, U>(U obj, List<string>? excludes = null) where T : new()
        {
            try
            {
                bool toContinue = false;
                T objT1 = new T();

                foreach (var item in objT1.GetType().GetProperties())
                {
                    // Checks the properties that were marked as excluded from binding
                    /*******/
                    if (excludes != null)
                    {
                        (bool flowControl, toContinue) = ExecuteExcludes(excludes, toContinue, item);
                        if (!flowControl)
                        {
                            continue;
                        }
                    }
                    /*******/

                    if (obj != null)
                    {
                        foreach (var item2 in obj.GetType().GetProperties())
                        {
                            try
                            {
                                // Checks the properties that were marked as excluded from binding
                                /*******/
                                if (excludes != null)
                                {
                                    (bool flowControl, toContinue) = ExecuteExcludes(excludes, toContinue, item);
                                    if (!flowControl)
                                    {
                                        continue;
                                    }
                                }
                                /*******/

                                if (item2.GetValue(obj, null) == null)
                                    continue;

                                if (item.Name.Equals(item2.Name, StringComparison.InvariantCultureIgnoreCase) &&
                                    item.PropertyType.Name.Equals(item2.PropertyType.Name, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    item.SetValue(objT1, item2.GetValue(obj, null), null);
                                    break;
                                }
                                else if (item.Name.Equals(item2.Name, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    if (item.PropertyType.BaseType != null && string.Equals(item.PropertyType.BaseType.Name, "Enum", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        var value = item2.GetValue(obj, null)?.ToString();
                                        if (value != null)
                                        {
                                            item.SetValue(objT1, Enum.Parse(item.PropertyType, value, true), null);
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        item.SetValue(objT1, item2.GetValue(obj, null), null);
                                        break;
                                    }
                                }
                            }
                            catch (ArgumentException) { }
                        }
                    }
                }

                return objT1;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while populating properties! " + ex.Message);
            }
        }

        private static (bool flowControl, bool value) ExecuteExcludes(List<string>? excludes, bool toContinue, PropertyInfo item)
        {
            if (excludes == null)
            {
                excludes = new List<string> { "Id", "EntityKey" };
            }
            else
            {

                foreach (string exclude in excludes)
                {
                    if (item.Name.Equals(exclude, StringComparison.InvariantCultureIgnoreCase))
                    {
                        toContinue = true;
                        break;
                    }
                }
                if (toContinue)
                {
                    toContinue = false;
                    return (flowControl: false, value: default);
                }
            }
            return (flowControl: true, value: default);
        }

        /// <summary>
        /// ************* POPULATE ENTITY PROPERTIES ******************
        /// Method that performs property binding for properties belonging to a Context entity.
        /// CAUTION!! THIS METHOD RETURNS A NEW OBJECT
        /// </summary>
        /// <typeparam name="T">Object that will be populated.</typeparam>
        /// <typeparam name="U">Type of object to be copied.</typeparam>
        /// <param name="obj">Instance of the object to be copied.</param>
        /// <param name="excludes">List of Properties that will be excluded from the Bind. (Id and EntityKey are already included in the bind exclusion list)</param>
        /// <returns>Populated object.</returns>
        //[DebuggerStepThrough]
        public static T PopulateEntityProperties<T, U>(U obj, List<string>? excludes = null) where T : new()
        {
            try
            {
                bool dontStop = false;
                T objT1 = new T();
                if (excludes == null)
                {
                    excludes = new List<string> { "Id", "EntityKey" };
                }
                else
                {
                    excludes.Add("Id");
                    excludes.Add("EntityKey");
                }
                Execute(obj, ref excludes, ref dontStop, objT1);
                return objT1;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while populating properties! " + ex.Message);
            }
        }

        /// <summary>
        /// ************* POPULATE ENTITY PROPERTIES ******************
        /// CAUTION! This method returns the SAME object that was passed as a parameter
        /// Method that performs property binding for properties belonging to a Context entity.
        /// </summary>
        /// <typeparam name="TOrigin">Type of object to be copied.</typeparam>
        /// <typeparam name="TDestiny">Object that will be populated.</typeparam>
        /// <param name="origin">Instance of the object to be copied.</param>
        /// /// <param name="destiny">Instance of the object that will receive values from the obj (previous parameter).</param>
        /// <param name="excludes">List of Properties that will be excluded from the Bind. (Id and EntityKey are already included in the bind exclusion list)</param>
        /// <returns>Populated object that can be used by a third object.</returns>
        //[DebuggerStepThrough]
        public static TDestiny PopulateEntityProperties<TOrigin, TDestiny>(TOrigin origin, TDestiny destiny, List<string>? excludes = null) where TDestiny : new()
        {
            try
            {
                bool dontStop = false;
                NewExecute(origin, ref excludes, ref dontStop, destiny);
                return destiny;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while populating properties! " + ex.Message);
            }
        }

        private static void NewExecute<TDestiny, TOrigin>(TOrigin origin, ref List<string>? excludes, ref bool dontStop, TDestiny destiny) where TDestiny : new()
        {
            if (origin == null)
            {
                throw new ArgumentNullException(nameof(origin), "The origin object cannot be null.");
            }
            foreach (var originItem in origin.GetType().GetProperties())
            {
                try
                {
                    string baseType2 = originItem.PropertyType.BaseType?.Name ?? string.Empty;
                    if (string.Equals(baseType2, "EntityObject", StringComparison.InvariantCultureIgnoreCase) ||
                        string.Equals(baseType2, "EntityReference", StringComparison.InvariantCultureIgnoreCase) ||
                        string.Equals(baseType2, "RelatedEnd", StringComparison.InvariantCultureIgnoreCase))
                    {
                        continue;
                    }
                    // Checks the properties that were marked as excluded from binding
                    /*******/
                    if (excludes != null)
                    {
                        foreach (string exclude in excludes)
                        {
                            if (originItem.Name.Equals(exclude, StringComparison.InvariantCultureIgnoreCase))
                            {
                                dontStop = true;
                                break;
                            }
                        }
                        if (dontStop)
                        {
                            dontStop = false;
                            continue;
                        }
                    }

                    var destinyItem = destiny?.GetType().GetProperty(originItem.Name);
                    if (destinyItem == null)
                        continue;
                    if (destinyItem == null)
                        continue;
                    if (destinyItem.Name.Equals(originItem.Name, StringComparison.InvariantCultureIgnoreCase) &&
                        destinyItem.PropertyType.Name.Equals(originItem.PropertyType.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        destinyItem.SetValue(destiny, originItem.GetValue(origin, null), null);
                        continue;
                    }
                    else if (destinyItem.Name.Equals(originItem.Name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (string.Equals(destinyItem?.PropertyType?.BaseType?.Name, "Enum", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var originValue = originItem.GetValue(origin, null)?.ToString();
                            if (!string.IsNullOrEmpty(originValue))
                            {
                                destinyItem?.SetValue(destiny, Enum.Parse(destinyItem.PropertyType, originValue, true), null);
                            }
                            continue;
                        }
                        else
                        {
                            destinyItem?.SetValue(destiny, originItem.GetValue(origin, null), null);
                            continue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private static void Execute<TDestiny, TOrigin>(TOrigin origin, ref List<string> excludes, ref bool dontStop, TDestiny destiny) where TDestiny : new()
        {
            if (destiny != null)
            {
                foreach (var item in destiny.GetType().GetProperties())
                {
                    try
                    {
                        string baseType = item.PropertyType.BaseType?.Name ?? string.Empty;
                        if (string.Equals(baseType, "EntityObject", StringComparison.InvariantCultureIgnoreCase) ||
                            string.Equals(baseType, "EntityReference", StringComparison.InvariantCultureIgnoreCase) ||
                            string.Equals(baseType, "RelatedEnd", StringComparison.InvariantCultureIgnoreCase))
                        {
                            continue;
                        }
                        // Checks the properties that were marked as excluded from binding
                        /*******/
                        if (excludes != null)
                        {
                            foreach (string exclude in excludes)
                            {
                                if (item.Name.Equals(exclude, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    dontStop = true;
                                    break;
                                }
                            }
                            if (dontStop)
                            {
                                dontStop = false;
                                continue;
                            }
                        }
                        /*******/

                        if (origin != null)
                        {


                            foreach (var item2 in origin.GetType().GetProperties())
                            {
                                try
                                {
                                    string baseType2 = item2.PropertyType.BaseType?.Name ?? string.Empty;
                                    if (string.Equals(baseType2, "EntityObject", StringComparison.InvariantCultureIgnoreCase) ||
                                        string.Equals(baseType2, "EntityReference", StringComparison.InvariantCultureIgnoreCase) ||
                                        string.Equals(baseType2, "RelatedEnd", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        continue;
                                    }

                                    // Checks the properties that were marked as excluded from binding
                                    /*******/
                                    if (excludes != null)
                                    {
                                        foreach (string exclude in excludes)
                                        {
                                            if (item2.Name.Equals(exclude, StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                dontStop = true;
                                                break;
                                            }
                                        }
                                        if (dontStop)
                                        {
                                            dontStop = false;
                                            continue;
                                        }
                                    }
                                    /*******/

                                    if (item2.GetValue(origin, null) == null)
                                        continue;
                                    if (item.Name.Equals(item2.Name, StringComparison.InvariantCultureIgnoreCase) &&
                                        item.PropertyType.Name.Equals(item2.PropertyType.Name, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        item.SetValue(destiny, item2.GetValue(origin, null), null);
                                        break;
                                    }
                                    else if (item.Name.Equals(item2.Name, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (string.Equals(item?.PropertyType?.BaseType?.Name, "Enum", StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            var value = item2.GetValue(origin, null)?.ToString();
                                            if (!string.IsNullOrEmpty(value))
                                            {
                                                item?.SetValue(destiny, Enum.Parse(item.PropertyType, value, true), null);
                                            }
                                            break;
                                        }
                                        else
                                        {
                                            item?.SetValue(destiny, item2.GetValue(origin, null), null);
                                            break;
                                        }
                                    }
                                }
                                catch (ArgumentException) { }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Populates a property according to its name.
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="obj">Object with the property to be populated</param>
        /// <param name="propertyName">Name of the property to be populated</param>
        /// <param name="value">Value to be inserted into the property</param>
        /// <returns>Object with the property set</returns>
        [DebuggerStepThrough]
        public static T PopulateProperty<T>(T obj, string propertyName, string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return obj;

                var item = obj?.GetType().GetProperty(propertyName);

                if (string.Equals(item?.PropertyType?.BaseType?.Name, "Enum", StringComparison.InvariantCultureIgnoreCase))
                {
                    item?.SetValue(obj, Enum.Parse(item.PropertyType, value, true), null);
                }
                else if (string.Equals(item?.PropertyType.Name, "Boolean", StringComparison.InvariantCultureIgnoreCase))
                {
                    item?.SetValue(obj, Boolean.Parse(value), null);
                }
                else if (string.Equals(item?.PropertyType.Name, "Int32", StringComparison.InvariantCultureIgnoreCase))
                {
                    item?.SetValue(obj, int.Parse(value), null);
                }
                else if (string.Equals(item?.PropertyType.Name, "String", StringComparison.InvariantCultureIgnoreCase))
                {
                    item?.SetValue(obj, value, null);
                }
                else if (!string.Equals(item?.PropertyType?.BaseType?.Name, "Object", StringComparison.InvariantCultureIgnoreCase))
                {
                    item?.SetValue(obj, value, null);
                }

                return obj;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while populating property! " + ex.Message);
            }
        }

        /// <summary>
        /// Gets a property value according to its name.
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="obj">Object with the property to get value from</param>
        /// <param name="propertyName">Name of the property to get value from</param>
        /// <returns>Object with the property value</returns>
        [DebuggerStepThrough]
        public static object? GetValueProperty<T>(T obj, string propertyName)
        {
            try
            {
                if (string.IsNullOrEmpty(propertyName))
                    return null;

                var item = obj?.GetType().GetProperty(propertyName);

                return item?.GetValue(obj, null);
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error while getting property value! " + ex.Message);
            }
        }

        [DebuggerStepThrough]
        public static T? Binder<T>(Object myobj) where T : new()
        {
            Type target = typeof(T);
            var x = Activator.CreateInstance(target, false);
            var d = from source in target.GetMembers().ToList() where source.MemberType == MemberTypes.Property select source;
            List<MemberInfo> members = d.Where(memberInfo => d.Select(c => c.Name).ToList().Contains(memberInfo.Name)).ToList();
            PropertyInfo? propertyInfo;
            object? value;
            foreach (var memberInfo in members)
            {
                propertyInfo = typeof(T).GetProperty(memberInfo.Name);
                value = myobj?.GetType()?.GetProperty(memberInfo.Name)?.GetValue(myobj, null);

                propertyInfo?.SetValue(x, value, null);
            }
            if (x != null)
            {
                return (T)x;
            }
            else
                return default(T);

        }

        public static List<T> ConvertDataTableToList<T>(DataTable dt) where T : new()
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }

        private static T GetItem<T>(DataRow dr) where T : new()
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (string.Equals(pro.Name, column.ColumnName, StringComparison.OrdinalIgnoreCase))
                    {
                        var cell = dr[column.ColumnName];

                        if (pro.PropertyType == typeof(int))
                        {
                            int.TryParse(cell.ToString(), out int value);
                            pro.SetValue(obj, value, null);
                        }
                        else if (pro.PropertyType == typeof(long))
                        {
                            long.TryParse(cell.ToString(), out long value);
                            pro.SetValue(obj, value, null);
                        }
                        else if (pro.PropertyType == typeof(short))
                        {
                            short.TryParse(cell.ToString(), out short value);
                            pro.SetValue(obj, value, null);
                        }
                        else if (pro.PropertyType == typeof(decimal))
                        {
                            decimal.TryParse(cell.ToString(), out decimal value);
                            pro.SetValue(obj, value, null);
                        }
                        else if (pro.PropertyType == typeof(bool))
                        {
                            pro.SetValue(obj, cell.ToString() == "1", null);
                        }
                        else if (pro.PropertyType == typeof(string))
                        {
                            pro.SetValue(obj, cell.ToString(), null);
                        }
                        else
                        {
                            pro.SetValue(obj, cell is DBNull ? null : cell, null);
                        }

                        break;
                    }
                }
            }
            return obj;
        }
    }

}

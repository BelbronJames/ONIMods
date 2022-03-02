﻿/*
 * Copyright 2022 Peter Han
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
 * BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using HarmonyLib;
using PeterHan.PLib.Core;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace PeterHan.FastTrack {
	/// <summary>
	/// Extension methods make life easier!
	/// </summary>
	public static class ExtensionMethods {
		/// <summary>
		/// Generates a getter for a type that is not known at compile time. The getter will
		/// be emitted as a non-type checked function that accepts an object and blindly
		/// attempts to retrieve the field type. Use with caution!
		/// </summary>
		/// <typeparam name="D">The field type to return.</typeparam>
		/// <param name="type">The containing type of the field.</param>
		/// <param name="fieldName">The field name.</param>
		/// <returns>A delegate that can access that field.</returns>
		public static Func<object, D> GenerateGetter<D>(this Type type, string fieldName)
				where D : class {
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			if (string.IsNullOrEmpty(fieldName))
				throw new ArgumentNullException(nameof(fieldName));
			var field = type.GetField(fieldName, PPatchTools.BASE_FLAGS | BindingFlags.
				Instance | BindingFlags.Static);
			if (field == null)
				throw new ArgumentException("No such field: {0}.{1}".F(type.FullName,
					fieldName));
			if (!typeof(D).IsAssignableFrom(field.FieldType))
				throw new ArgumentException("Field type {0} does not match desired {1}".F(
					field.FieldType.FullName, typeof(D).FullName));
			var getter = new DynamicMethod(fieldName + "_GetDelegate", typeof(D), new Type[] {
				typeof(object)
			}, true);
			var generator = getter.GetILGenerator();
			// Getter will load the first argument and use ldfld/ldsfld
			if (field.IsStatic)
				generator.Emit(OpCodes.Ldsfld, field);
			else {
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Ldfld, field);
			}
			generator.Emit(OpCodes.Ret);
#if DEBUG
			PUtil.LogDebug("Created delegate for field {0}.{1} with type {2}".
				F(type.FullName, fieldName, typeof(D).FullName));
#endif
			return getter.CreateDelegate(typeof(Func<object, D>)) as Func<object, D>;
		}
	}
}

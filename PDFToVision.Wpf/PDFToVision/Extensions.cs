//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="https://github.com/jhueppauff/PDFToVision">
// Copyright 2018 Jhueppauff
// MIT License
// For licence details visit https://github.com/jhueppauff/PDFToVision/blob/master/LICENSE
// </copyright>
//-----------------------------------------------------------------------

namespace PDFToVision
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extensions Class
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Foreach extention method
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ie"></param>
        /// <param name="action"></param>
        public static void ForEach<T>(this IEnumerable<T> ie, Action<T> action)
        {
            foreach (var i in ie)
            {
                action(i);
            }
        }
    }
}

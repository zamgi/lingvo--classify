using System;
using System.Collections.Generic;

namespace lingvo.classify
{
    /// <summary>
    /// 
    /// </summary>
    unsafe internal static class VectorsArithmetic
    {
        private static double ScalarProductOfVector( float[] v1, float[] v2 )
        {
            if ( v1.Length != v2.Length )
                throw (new InvalidOperationException());

            double scalar_product = 0;
            fixed ( float* ptr1 = v1 )
            fixed ( float* ptr2 = v2 )
            {
                for ( int i = 0, len = v1.Length; i < len ; i++ )
                {
                    scalar_product += *(ptr1 + i) * *(ptr2 +  i);
                }
            }
            return (scalar_product);
        }

        private static double VectorSquareLength( float[] v )
        {
            double scalar_product = 0;
            fixed ( float* ptr = v )
            {
                for ( int i = 0, len = v.Length; i < len ; i++ )
                {
                    var f = *(ptr + i);
                    scalar_product += f * f;
                }
            }
            return (scalar_product); //Math.Sqrt(scalar_product));
        }
        
		public static double VectorSquareLength< TKey >( Dictionary< TKey, float[] >.ValueCollection values, int classIndex )
        {
            double scalar_product = 0;
            foreach ( var value in values )
            {
				var f = value[ classIndex ];
                scalar_product += f * f;
            }
            return (scalar_product); //Math.Sqrt(scalar_product));
        }
        public static double VectorSquareLength< TKey >( Dictionary< TKey, IntPtr >.ValueCollection values, int classIndex )
        {
            double scalar_product = 0;
            foreach ( var value in values )
            {
                var weightClassesBytePtr = ((byte*) value) + 1;
                var weightClassesFloatPtr = (float*) weightClassesBytePtr;

                var f = weightClassesFloatPtr[ classIndex ];
                scalar_product += f * f;
            }
            return (scalar_product); //Math.Sqrt(scalar_product));
        }

        /*public static double CosBetweenVectors( float[] v1, float[] v2 )
        {
            var spv = ScalarProductOfVector( v1, v2 );

            var v1_sq_len = VectorSquareLength( v1 );
            var v2_sq_len = VectorSquareLength( v2 );

            var cos = spv / Math.Sqrt( v1_sq_len * v2_sq_len );
            return (cos);
        }*/
        /*public static double CosBetweenVectors( float[] v1, double v1SqrtLength, float[] v2 )
        {
            var spv = ScalarProductOfVector( v1, v2 );

            //var v1_sq_len = VectorSquareLength( v1 );
            var v2_sq_len = VectorSquareLength( v2 );

            var cos = spv / Math.Sqrt( v1SqrtLength * v2_sq_len );
            return (cos);
        }*/
    }
}

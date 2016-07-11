#ifndef SNOW_INCLUDED
#define SNOW_INCLUDED



#define GAMMA 5.828427124 // FOUR_GAMMA_SQUARED = sqrt(8)+3;
#define CSTAR 0.923879532 // cos(pi/8)
#define SSTAR 0.3826834323 // sin(p/8)

struct quat
{
	float values[4];
};

//helper functions
//gets only recompiled if there are changes in the including file!
float bspline(float input)
{
	input = abs(input);
	float w;
	if (input < 1)
		w = input*input*(input / 2 - 1) + 2 / 3.0;
	else if (input < 2)
		w = input*(input*(-input / 6 + 1) - 2) + 4 / 3.0;
	else return 0;
	if (w < 0)
		return 0;
	return w;

}

float bsplineSlope(float input)
{
	float abs_input = abs(input);
	float w;

	if (abs_input < 1)
		return 1.5*input*abs_input - 2 * input;
	else if (input < 2)
		return -input*abs_input / 2 + 2 * input - 2 * input / abs_input;
	else return 0;
}

float3x3 outerProduct(float3 v, const float3 w)
{
	return float3x3(v.x*w.x, v.y*w.x, v.z*w.x,
		v.x*w.y, v.y*w.y, v.z*w.y,
		v.x*w.z, v.y*w.z, v.z*w.z);
}

float product(float4 input)
{
	return input.x * input.y * input.z * input.w;
}

float3x3 diag_sum(float3x3 input, const float c)
{
	for (int i=0; i<3; i++)
		input[i][i] += c;

	return input;
}

float3x3 multiplyAtB(float3x3 A, float3x3 B)
{
		float3x3 tmp;
        tmp[0] = A[0]*B[0] + A[1]*B[1] + A[2]*B[2];
        tmp[1] = A[3]*B[0] + A[4]*B[1] + A[5]*B[2];
        tmp[2] = A[6]*B[0] + A[7]*B[1] + A[8]*B[2];
        tmp[3] = A[0]*B[3] + A[1]*B[4] + A[2]*B[5];
        tmp[4] = A[3]*B[3] + A[4]*B[4] + A[5]*B[5];
        tmp[5] = A[6]*B[3] + A[7]*B[4] + A[8]*B[5];
        tmp[6] = A[0]*B[6] + A[1]*B[7] + A[2]*B[8];
        tmp[7] = A[3]*B[6] + A[4]*B[7] + A[5]*B[8];
        tmp[8] = A[6]*B[6] + A[7]*B[7] + A[8]*B[8];
		return tmp;
}

float3x3  MatfromQuat( quat q )
{
	float x = q.values[0];
	float y = q.values[1];
	float z = q.values[2];
	float w = q.values[3];


    float qxx = x*x;
    float qyy = y*y;
    float qzz = z*z;
    float qxz = x*z;
    float qxy = x*y;
    float qyz = y*z;
    float qwx = w*x;
    float qwy = w*y;
    float qwz = w*z;

    float3x3 M;

    M[0][0] = 1.f - 2.f*(qyy+qzz);
    M[1][0] = 2.f * (qxy+qwz);
    M[2][0] = 2.f * (qxz-qwy);
    M[0][1] = 2.f * (qxy-qwz);
    M[1][1] = 1.f - 2.f*(qxx+qzz);
    M[2][1] = 2.f * (qyz+qwx);
    M[0][2] = 2.f * (qxz+qwy);
    M[1][2] = 2.f * (qyz-qwx);
    M[2][2] = 1.f - 2.f*(qxx+qyy);
    return M;
}


void jacobiConjugation( int x, int y, int z, out float3x3 S, out quat qV )
{
	float ch = 2.f * (S[0][0]-S[1][1]);
	float ch2 = ch*ch;
    float sh = S[0][1];
	float sh2 = sh*sh;
    bool flag = ( GAMMA * sh2 < ch2 );
    float w = rsqrt( ch2 + sh2 );
    ch = flag ? w*ch : CSTAR; 
	ch2 = ch*ch;
    sh = flag ? w*sh : SSTAR; 
	sh2 = sh*sh;

    // build rotation matrix Q
    float scale = 1.f / (ch2 + sh2);
    float a = (ch2-sh2) * scale;
    float b = (2.f*sh*ch) * scale;
    float a2 = a*a, b2 = b*b, ab = a*b;

    // Use what we know about Q to simplify S = Q' * S * Q
    // and the re-arranging step.
    float s0 = a2*S[0][0] + 2*ab*S[1][0] + b2*S[1][1];
    float s2 = a*S[0][2] + b*S[1][2];
    float s3 = (a2-b2)*S[0][0] + ab*(S[1][1]-S[0][0]);
    float s4 = b2*S[0][0] - 2*ab*S[0][1] + a2*S[1][1];
    float s5 = a*S[1][2] - b*S[0][2];
    float s8 = S[2][2];

    S = float3x3( s4, s5, s3,
              s5, s8, s2,
              s3, s2, s0 );

    float tmp[3] = { sh*qV.values[0], sh*qV.values[1], sh*qV.values[2] };
    sh *= qV.values[4];
    // original
    qV.values[0] *= ch;
	qV.values[1] *= ch;
	qV.values[2] *= ch;
	qV.values[3] *= ch;

    qV.values[z] += sh;
    qV.values[3] -= tmp[z];
    qV.values[x] += tmp[y];
	qV.values[y] -= tmp[x];

}


void jacobiEigenanalysis( out float3x3 S, out quat qV )
{
    qV.values[0] = 1.0f;

    jacobiConjugation( 0, 1, 2, S, qV );
    jacobiConjugation( 1, 2, 0, S, qV );
    jacobiConjugation( 2, 0, 1, S, qV );

    jacobiConjugation( 0, 1, 2, S, qV );
    jacobiConjugation( 1, 2, 0, S, qV );
    jacobiConjugation( 2, 0, 1, S, qV );

    jacobiConjugation( 0, 1, 2, S, qV );
    jacobiConjugation( 1, 2, 0, S, qV );
    jacobiConjugation( 2, 0, 1, S, qV );

    jacobiConjugation( 0, 1, 2, S, qV );
    jacobiConjugation( 1, 2, 0, S, qV );
    jacobiConjugation( 2, 0, 1, S, qV );
}

float3 getMatrixColumn(float3x3 mat, int index)
{
	return float3(mat[0][index], mat[1][index], mat[2][index]);
}

void condSwap(bool c, out float x, out float y, int negative)
{
    float _x_ = x * negative;  
    x = c ? y : x;
	y = c ? _x_ : y; 
}

void condSwap(bool c, out float3 x, out float3 y, int negative)
{
    float3 _x_ = x * negative;  
    x = c ? y : x;
	y = c ? _x_ : y; 
}

float3x3 buildMatrixFromColumn(float3 col1, float3 col2, float3 col3)
{
	float3x3 mat = float3x3(col1, col2, col3);
	mat = transpose(mat);

	return mat;
}

void sortSingularValues(float3x3 B, float3x3 V)
{

	float3 b1 = getMatrixColumn(B,0); float3 v1 = getMatrixColumn(V,0);
    float3 b2 = getMatrixColumn(B,1); float3 v2 = getMatrixColumn(V,1);
    float3 b3 = getMatrixColumn(B,2); float3 v3 = getMatrixColumn(V,2);
    float rho1 = dot( b1, b1 );
    float rho2 = dot( b2, b2 );
    float rho3 = dot( b3, b3 );
    bool c;

    c = rho1 < rho2;
    condSwap( c, b1, b2, -1); 
    condSwap( c, v1, v2, -1);
    condSwap( c, rho1, rho2, 1);

    c = rho1 < rho3;
    condSwap( c, b1, b3, -1); 
    condSwap( c, v1, v3, -1);
    condSwap( c, rho1, rho3, 1);

    c = rho2 < rho3;
    condSwap( c, b2, b3, -1); 
    condSwap( c, v2, v3, -1);

    // re-build B,V
    B = buildMatrixFromColumn( b1, b2, b3 );
	V = buildMatrixFromColumn( v1, v2, v3 );
	
}

void QRGivensQuaternion( float a1, float a2, out float ch, out float sh )
{
    // a1 = pivot point on diagonal
    // a2 = lower triangular entry we want to annihilate
    float rho = sqrt( a1*a1 + a2*a2 );

    sh = rho > EPSILON ? a2 : 0;
    ch = abs(a1) + max( rho, EPSILON );
    bool b = a1 < 0;
    condSwap( b, sh, ch, 1 );
    float w = rsqrt( ch*ch + sh*sh );

    ch *= w;
    sh *= w;
}

void QRDecomposition( float3x3 B, float3x3 Q, float3x3 R )
{
    R = B;

    // QR decomposition of 3x3 matrices using Givens rotations to
    // eliminate elements B21, B31, B32
    quat qQ; // cumulative rotation
    float3x3 U;
    float ch, sh, s0, s1;

    // first givens rotation
    QRGivensQuaternion( R[0][0], R[1][0], ch, sh );

    s0 = 1-2*sh*sh;
    s1 = 2*sh*ch;
    U = float3x3(  s0, s1, 0,
              -s1, s0, 0,
                0,  0, 1 );

    R = transpose(U) * R; //mat3::multiplyAtB( U, R );

    // update cumulative rotation
    //qQ = quat( ch*qQ.w-sh*qQ.z, ch*qQ.x+sh*qQ.y, ch*qQ.y-sh*qQ.x, sh*qQ.w+ch*qQ.z );

	qQ.values[0] = ch * qQ.values[3] + sh * qQ.values[1];
	qQ.values[1] = ch * qQ.values[0] + sh * qQ.values[2];
	qQ.values[2] = ch * qQ.values[1] - sh * qQ.values[3];
	qQ.values[3] = ch * qQ.values[2] - sh * qQ.values[0];

    // second givens rotation
    QRGivensQuaternion( R[0], R[2], ch, sh );

    s0 = 1 -2 * sh * sh;
    s1 = 2 * sh * ch;
    U = float3x3(  s0, 0, s1,
                0, 1,  0,
              -s1, 0, s0 );

    R = mat3::multiplyAtB( U, R );

    // update cumulative rotation
    qQ = quat( ch*qQ.w+sh*qQ.y, ch*qQ.x+sh*qQ.z, ch*qQ.y-sh*qQ.w, ch*qQ.z-sh*qQ.x );
	qQ.values[0] = ch*qQ.w+sh*qQ.y;
	qQ.values[1] = ch*qQ.x+sh*qQ.z;
	qQ.values[2] = ch*qQ.y-sh*qQ.w;
	qQ.values[3] = ch*qQ.z-sh*qQ.x;

    // third Givens rotation
    QRGivensQuaternion( R[4], R[5], ch, sh );

    s0 = 1-2*sh*sh;
    s1 = 2*sh*ch;
    U = mat3( 1,   0,  0,
              0,  s0, s1,
              0, -s1, s0 );

    R = mat3::multiplyAtB( U, R );

    // update cumulative rotation
    qQ = quat( ch*qQ.w-sh*qQ.x, sh*qQ.w+ch*qQ.x, ch*qQ.y+sh*qQ.z, ch*qQ.z-sh*qQ.y );

    // qQ now contains final rotation for Q
    Q = mat3::fromQuat(qQ);
}


void computeSVD( float3x3 A, out float3x3 W, out float3x3 S, out float3x3 V )
{
    // normal equations matrix
    float3x3 ATA = transpose(A) * A;

/// 2. Symmetric Eigenanlysis
    quat qV;

	
    jacobiEigenanalysis( ATA, qV );

    V = MatfromQuat(qV);

    float3x3 B = A * V;

/// 3. Sorting the singular values (find V)
    sortSingularValues( B, V );

/// 4. QR decomposition
    //QRDecomposition( B, W, S );
}

#endif
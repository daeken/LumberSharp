scene = RaymarchedScene('''
#version 410
precision highp float;
uniform vec3 uCameraPosition;
uniform mat4 uCameraMatrix;
in vec2 vPosition;
out vec4 normalDepth;

// The MIT License
// Copyright 2013 Inigo Quilez
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    

// A list of useful distance function to simple primitives, and an example on how to 
// do some interesting boolean operations, repetition and displacement.
//
// More info here: http://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm

//------------------------------------------------------------------

float sdPlane(vec3 p) {
	return p.y;
}

float sdSphere(vec3 p, float s) {
    return length(p)-s;
}

float sdBox(vec3 p, vec3 b) {
    vec3 d = abs(p) - b;
    return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
}

float sdEllipsoid(in vec3 p, in vec3 r) {
    float k0 = length(p/r);
    float k1 = length(p/(r*r));
    return k0*(k0-1.0)/k1;
    
}

float sdRoundBox(in vec3 p, in vec3 b, in float r) {
    vec3 q = abs(p) - b;
    return min(max(q.x,max(q.y,q.z)),0.0) + length(max(q,0.0)) - r;
}


float sdTorus(vec3 p, vec2 t) {
    return length(vec2(length(p.xz)-t.x,p.y))-t.y;
}

float sdHexPrism(vec3 p, vec2 h) {
    vec3 q = abs(p);
    const vec3 k = vec3(-0.8660254, 0.5, 0.57735);
    p = abs(p);
    p.xy -= 2.0*min(dot(k.xy, p.xy), 0.0)*k.xy;
    vec2 d = vec2(
       length(p.xy - vec2(clamp(p.x, -k.z*h.x, k.z*h.x), h.x))*sign(p.y - h.x),
       p.z-h.y);
    return min(max(d.x,d.y),0.0) + length(max(d,0.0));
}

float sdCapsule(vec3 p, vec3 a, vec3 b, float r) {
	vec3 pa = p-a, ba = b-a;
	float h = clamp(dot(pa,ba)/dot(ba,ba), 0.0, 1.0);
	return length(pa - ba*h) - r;
}

float sdRoundCone(in vec3 p, in float r1, float r2, float h) {
    vec2 q = vec2(length(p.xz), p.y);
    
    float b = (r1-r2)/h;
    float a = sqrt(1.0-b*b);
    float k = dot(q,vec2(-b,a));
    
    if(k < 0.0) return length(q) - r1;
    if(k > a*h) return length(q-vec2(0.0,h)) - r2;
        
    return dot(q, vec2(a,b)) - r1;
}

float dot2(in vec3 v) {return dot(v,v);}
float sdRoundCone(vec3 p, vec3 a, vec3 b, float r1, float r2) {
    // sampling independent computations (only depend on shape)
    vec3  ba = b - a;
    float l2 = dot(ba,ba);
    float rr = r1 - r2;
    float a2 = l2 - rr*rr;
    float il2 = 1.0/l2;
    
    // sampling dependant computations
    vec3 pa = p - a;
    float y = dot(pa,ba);
    float z = y - l2;
    float x2 = dot2(pa*l2 - ba*y);
    float y2 = y*y*l2;
    float z2 = z*z*l2;

    // single square root!
    float k = sign(rr)*rr*rr*x2;
    if(sign(z)*a2*z2 > k) return  sqrt(x2 + z2)        *il2 - r2;
    if(sign(y)*a2*y2 < k) return  sqrt(x2 + y2)        *il2 - r1;
                            return (sqrt(x2*a2*il2)+y*rr)*il2 - r1;
}


float sdEquilateralTriangle(in vec2 p) {
    const float k = 1.73205;//sqrt(3.0);
    p.x = abs(p.x) - 1.0;
    p.y = p.y + 1.0/k;
    if(p.x + k*p.y > 0.0) p = vec2(p.x - k*p.y, -k*p.x - p.y)/2.0;
    p.x += 2.0 - 2.0*clamp((p.x+2.0)/2.0, 0.0, 1.0);
    return -length(p)*sign(p.y);
}

float sdTriPrism(vec3 p, vec2 h) {
    vec3 q = abs(p);
    float d1 = q.z-h.y;
    h.x *= 0.866025;
    float d2 = sdEquilateralTriangle(p.xy/h.x)*h.x;
    return length(max(vec2(d1,d2),0.0)) + min(max(d1,d2), 0.);
}

// vertical
float sdCylinder(vec3 p, vec2 h) {
  vec2 d = abs(vec2(length(p.xz),p.y)) - h;
  return min(max(d.x,d.y),0.0) + length(max(d,0.0));
}

// arbitrary orientation
float sdCylinder(vec3 p, vec3 a, vec3 b, float r) {
    vec3 pa = p - a;
    vec3 ba = b - a;
    float baba = dot(ba,ba);
    float paba = dot(pa,ba);
    float x = length(pa*baba-ba*paba) - r*baba;
    float y = abs(paba-baba*0.5)-baba*0.5;
    float x2 = x*x;
    float y2 = y*y*baba;
    float d = (max(x,y)<0.0)?-min(x2,y2):(((x>0.0)?x2:0.0)+((y>0.0)?y2:0.0));
    return sign(d)*sqrt(abs(d))/baba;
}

float sdCone(in vec3 p, in vec3 c) {
    vec2 q = vec2(length(p.xz), p.y);
    float d1 = -q.y-c.z;
    float d2 = max(dot(q,c.xy), q.y);
    return length(max(vec2(d1,d2),0.0)) + min(max(d1,d2), 0.);
}

float dot2(in vec2 v) { return dot(v,v); }
float sdCappedCone(in vec3 p, in float h, in float r1, in float r2) {
    vec2 q = vec2(length(p.xz), p.y);
    
    vec2 k1 = vec2(r2,h);
    vec2 k2 = vec2(r2-r1,2.0*h);
    vec2 ca = vec2(q.x-min(q.x,(q.y < 0.0)?r1:r2), abs(q.y)-h);
    vec2 cb = q - k1 + k2*clamp(dot(k1-q,k2)/dot2(k2), 0.0, 1.0);
    float s = (cb.x < 0.0 && ca.y < 0.0) ? -1.0 : 1.0;
    return s*sqrt(min(dot2(ca),dot2(cb)));
}

// h = { cos a, sin a, height }
float sdPryamid4(vec3 p, vec3 h) {
    // Tetrahedron = Octahedron - Cube
    float box = sdBox(p - vec3(0,-2.0*h.z,0), vec3(2.0*h.z));
 
    float d = 0.0;
    d = max(d, abs(dot(p, vec3(-h.x, h.y, 0))));
    d = max(d, abs(dot(p, vec3(h.x, h.y, 0))));
    d = max(d, abs(dot(p, vec3(0, h.y, h.x))));
    d = max(d, abs(dot(p, vec3(0, h.y,-h.x))));
    float octa = d - h.z;
    return max(-box,octa); // Subtraction
 }

float length2(vec2 p) {
	return sqrt(p.x*p.x + p.y*p.y);
}

float length6(vec2 p) {
	p = p*p*p; p = p*p;
	return pow(p.x + p.y, 1.0/6.0);
}

float length8(vec2 p) {
	p = p*p; p = p*p; p = p*p;
	return pow(p.x + p.y, 1.0/8.0);
}

float sdTorus82(vec3 p, vec2 t) {
    vec2 q = vec2(length2(p.xz)-t.x,p.y);
    return length8(q)-t.y;
}

float sdTorus88(vec3 p, vec2 t) {
    vec2 q = vec2(length8(p.xz)-t.x,p.y);
    return length8(q)-t.y;
}

float sdCylinder6(vec3 p, vec2 h) {
    return max(length6(p.xz)-h.x, abs(p.y)-h.y);
}

//------------------------------------------------------------------

float opS(float d1, float d2) {
    return max(-d2,d1);
}

float opU(float d1, float d2) {
	return min(d1, d2);
}

vec3 opRep(vec3 p, vec3 c) {
    return mod(p,c)-0.5*c;
}

vec3 opTwist(vec3 p) {
    float  c = cos(10.0*p.y+10.0);
    float  s = sin(10.0*p.y+10.0);
    mat2   m = mat2(c,-s,s,c);
    return vec3(m*p.xz,p.y);
}

float pnoise_2d(vec2 v) {
	vec4 Pi = mod(floor(v.xyxy) + vec4(0., 0., 1., 1.), 289.);
	vec4 Pf = fract(v.xyxy) - vec4(0., 0., 1., 1.);
	vec4 gx = fract(mod(((mod((Pi.xzxz * 34. + 1.) * Pi.xzxz, 289.) + Pi.yyww) * 34. + 1.) * (mod((Pi.xzxz * 34. + 1.) * Pi.xzxz, 289.) + Pi.yyww), 289.) / 41.) * 2. - 1.;
	vec4 gy = abs(gx) - .5;
	gx -= floor(gx + .5);
	vec4 norm = inversesqrt(sqrt(gx * gx + gy * gy));
	vec2 fade_xy = ((Pf.xy * 6. - 15.) * Pf.xy + 10.) * pow(Pf.xy, vec2(2.));
	vec2 n_x = mix(vec2(dot(vec2(gx.x, gy.x) * norm.x, Pf.xy), dot(vec2(gx.z, gy.z) * norm.y, Pf.xw)), vec2(dot(vec2(gx.y, gy.y) * norm.z, Pf.zy), dot(vec2(gx.w, gy.w) * norm.w, Pf.zw)), fade_xy.x);
	return mix(n_x.x, n_x.y, fade_xy.y) * 2.3;
}

//------------------------------------------------------------------

float map(in vec3 pos) {
    float snoise = pnoise_2d(pos.xy * 9) * .05;
    float res = sdSphere(pos-vec3(0.0,0.25, 0.0), 0.25);// + snoise;
    res = opU(res, sdBox(pos-vec3(1.0,0.25, 0.0), vec3(0.25)));
    res = opU(res, sdRoundBox(pos-vec3(1.0,0.25, 1.0), vec3(0.15), 0.1));
	res = opU(res, sdTorus((pos + vec3(0, 0, sin(pos.x * 77) * .1))-vec3(0.0,0.25, 1.0), vec2(0.20,0.05)));
    res = opU(res, sdCapsule(pos-vec3(1.0,0.,-2.0),vec3(-0.1,0.1,-0.1), vec3(0.2,0.4,0.2), 0.1));
	res = opU(res, sdTriPrism(pos-vec3(-1.0,0.25,-1.0), vec2(0.25,0.05)));
	res = opU(res, sdCylinder(pos-vec3(1.0,0.30,-1.0), vec2(0.1,0.2)));
	res = opU(res, sdCone(pos-vec3(0.0,0.50,-1.0), vec3(0.8,0.6,0.3)));
	res = opU(res, sdTorus82(pos-vec3(0.0,0.25, 2.0), vec2(0.20,0.05)));
	res = opU(res, sdTorus88(pos-vec3(-1.0,0.25, 2.0), vec2(0.20,0.05)));
	res = opU(res, sdCylinder6(pos-vec3(1.0,0.30, 2.0), vec2(0.1,0.2)));
	res = opU(res, sdHexPrism(pos-vec3(-1.0,0.20, 1.0), vec2(0.25,0.05)));
	res = opU(res, sdPryamid4(pos-vec3(-1.0,0.15,-2.0), vec3(0.8,0.6,0.25)));
	res = opU(res, opS(sdRoundBox(pos-vec3(-2.0,0.2, 1.0), vec3(0.15),0.05),
	                           sdSphere(pos-vec3(-2.0,0.2, 1.0), 0.25)));
    res = opU(res, opS(sdTorus82(pos-vec3(-2.0,0.2, 0.0), vec2(0.20,0.1)),
	                           sdCylinder(opRep(vec3(atan(pos.x+2.0,pos.z)/6.2831, pos.y, 0.02+0.5*length(pos-vec3(-2.0,0.2, 0.0))), vec3(0.05,1.0,0.05)), vec2(0.02,0.6))));
	res = opU(res, 0.5*sdSphere(pos-vec3(-2.0,0.25,-1.0), 0.2) + 0.03*sin(50.0*pos.x)*sin(50.0*pos.y)*sin(50.0*pos.z));
	res = opU(res, 0.5*sdTorus(opTwist(pos-vec3(-2.0,0.25, 2.0)),vec2(0.20,0.05)));
    res = opU(res, sdCappedCone(pos-vec3(0.0,0.35,-2.0), 0.15, 0.2, 0.1));
    res = opU(res, sdEllipsoid(pos-vec3(-1.0,0.3,0.0), vec3(0.2, 0.25, 0.05)));
    res = opU(res, sdRoundCone(pos-vec3(-2.0,0.2,-2.0), 0.2, 0.1, 0.3));
    res = opU(res, sdCylinder(pos-vec3(2.0,0.2,-1.0), vec3(0.1,-0.1,0.0), vec3(-0.1,0.3,0.1), 0.08));
    res = opU(res, sdRoundCone(pos-vec3(2.0,0.2,-2.0), vec3(0.1,0.0,0.0), vec3(-0.1,0.3,0.1), 0.15, 0.05));
        
    return res;
}

float castRay(in vec3 ro, in vec3 rd) {
    float tmin = 1.0;
    float tmax = 20.0;
   
    float t = tmin;
    for(int i=0; i<64; i++) {
	    float precis = 0.0004 * t;
	    float res = map(ro + rd * t);
        if(res<precis || t>tmax) break;
        t += res;
    }

    return t <= tmax ? t : 100000;
}

vec3 calcNormal(in vec3 pos) {
    vec2 e = vec2(1.0,-1.0)*0.5773*0.0005;
    return normalize(e.xyy * map(pos + e.xyy) + 
					 e.yyx * map(pos + e.yyx) + 
					 e.yxy * map(pos + e.yxy) + 
					 e.xxx * map(pos + e.xxx));
}

vec4 render(in vec3 ro, in vec3 rd) { 
    float t = castRay(ro,rd);
    if(t != 100000) {
        vec3 pos = ro + t*rd;
        vec3 nor = calcNormal(pos);
        return vec4(nor, t);
    }

	return vec4(0);
}

void main() {
    normalDepth = render(uCameraPosition, (uCameraMatrix * vec4(normalize(vec3(vPosition.xy, 2.0)), 0)).xyz);
}
''')

scene.Width = 8000
scene.Height = 8000
#scene.Preview = True
#scene.EdgePreview = True

#page.Width = 297
#page.Height = 420

scene.Camera = PerspectiveCamera()
scene.Camera.Up = vec3(0, 1, 0)
direction = vec3(0, -3, -10)
scene.Camera.Position = vec3(-.5, 2, 6)
scene.Camera.LookAt = scene.Camera.Position - direction


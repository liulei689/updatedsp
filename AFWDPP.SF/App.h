#pragma once
typedef char			   int8;
typedef unsigned char      Uint8;
typedef int                int16;
typedef long               int32;
typedef long long          int64;
typedef unsigned int       Uint16;
typedef unsigned long      Uint32;
typedef unsigned long long Uint64;
typedef float              float32;
typedef long double        float64;
#define Dll_API __declspec(dllexport)
extern "C" Dll_API Uint8 add(Uint8 a, Uint8 b);

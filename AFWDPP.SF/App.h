#pragma once
#define TestDll_API __declspec(dllexport)
extern "C" TestDll_API int add(int a, int b);

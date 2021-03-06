// ModelMod: 3d data snapshotting & substitution program.
// Copyright(C) 2015,2016 John Quigley

// This program is free software : you can redistribute it and / or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 2.1 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with this program.If not, see <http://www.gnu.org/licenses/>.

#pragma once

#ifdef INTEROP_EXPORTS
#define INTEROP_API __declspec(dllexport)
#else
#define INTEROP_API __declspec(dllimport)
#endif

INTEROP_API int GetMMVersion();

#include "Types.h"

#define Code_AsyncLoadNotStarted 51
#define Code_AsyncLoadPending 52
#define Code_AsyncLoadInProgress 53
#define Code_AsyncLoadComplete 54

extern "C" {

struct IDirect3D9;
struct IDirect3DDevice9;
struct IDirect3DVertexBuffer9;
struct IDirect3DIndexBuffer9;
struct IDirect3DVertexDeclaration9;
struct IDirect3DIndexBuffer9;
struct IDirect3DBaseTexture9;
struct IDirect3DPixelShader9;

#define MaxModTextures 4
#define MaxModTexPathLen 8192 // Must match SizeConst attribute in managed code
typedef WCHAR ModPath[MaxModTexPathLen];

#pragma pack(push,8)
struct ModData {
	int modType;
	int primType;
	int vertCount;
	int primCount;
	int indexCount;
	int refVertCount;
	int refPrimCount;
	int declSizeBytes;
	int vertSizeBytes;
	int indexElemSizeBytes;
	ModPath texPath[MaxModTextures];
	ModPath pixelShaderPath;

	ModData() {
		memset(this, 0, sizeof(ModData));
	}
};
#pragma pack(pop)

#pragma pack(push,8)
struct SnapshotData {
	int primType;
	int baseVertexIndex;
	unsigned int minVertexIndex;
	unsigned int numVertices;
	unsigned int startIndex;
	unsigned int primCount;

	IDirect3DVertexDeclaration9* decl;
	IDirect3DIndexBuffer9* ib;

	SnapshotData() {
		memset(this,0,sizeof(SnapshotData));
	}
};
#pragma pack(pop)

#pragma pack(push,8)
struct ConfData {
	// Note: marshalling to bool requires [<MarshalAs(UnmanagedType.I1)>] on the field in managed code; otherwise it will try to marshall it as a 4 byte BOOL,
	// which has a detrimental effect on subsequent string fields!
	bool RunModeFull;
	bool LoadModsOnStart;
	char InputProfile[512];

	ConfData() {
		memset(this, 0, sizeof(ConfData));
	}
};
#pragma pack(pop)

typedef int (__stdcall *InitCallback) (int);
typedef ConfData* (__stdcall *SetPathsCB) (WCHAR*, WCHAR*);
typedef int (__stdcall *GetLoadingStateCB) ();
typedef int (__stdcall *LoadModDBCB) ();
typedef int (__stdcall *GetModCountCB) ();
typedef ModData* (__stdcall *GetModDataCB) (int modIndex);
typedef int (__stdcall *FillModDataCB) (int modIndex, char* declData, int declSize, char* vbData, int vbSize, char* ibData, int ibSize);
typedef int (__stdcall *TakeSnapshotCB) (IDirect3DDevice9* device, SnapshotData* snapdata);

#pragma pack(push,8)
typedef struct {
	SetPathsCB SetPaths;
	LoadModDBCB LoadModDB;
	GetModCountCB GetModCount;
	GetModDataCB GetModData;
	FillModDataCB FillModData;
	TakeSnapshotCB TakeSnapshot;
	GetLoadingStateCB GetLoadingState;
} ManagedCallbacks;
#pragma pack(pop)

#pragma pack(push,8)
struct NativeMemoryBuffer {
	ModelMod::Uint8* data;
	ModelMod::Int32 size;
};
#pragma pack(pop)

// Native memory buffer functions.
// Would prefer to define these as members, but that makes the struct have a non-standard layout and 
// thus unsafe to return over a C-style interop interface.
// Note that Init must be called manually on any buffer prior to calling alloc.
inline void InitNMB(NativeMemoryBuffer& nmb) {
	nmb.data = NULL;
	nmb.size = 0;
}
inline void ReleaseNMB(NativeMemoryBuffer& nmb) {
	delete[] nmb.data;
	nmb.data = NULL;
	nmb.size = 0;
}
inline void AllocNMB(NativeMemoryBuffer& nmb, ModelMod::Int32 size_) {
	ReleaseNMB(nmb);
	nmb.data = new ModelMod::Uint8[size_];
	nmb.size = size_;
}

INTEROP_API int OnInitialized(ManagedCallbacks* callbacks);
INTEROP_API void LogInfo(char* category, char* message);
INTEROP_API void LogWarn(char* category, char* message);
INTEROP_API void LogError(char* category, char* message);
INTEROP_API bool SaveTexture(int index, WCHAR* path);
INTEROP_API bool GetPixelShader(NativeMemoryBuffer* outBuf);

};

// This has no representation in managed code.
struct NativeModData {
	ModData modData;
	char* vbData;
	char* ibData;
	char* declData;
	IDirect3DVertexBuffer9* vb;
	IDirect3DIndexBuffer9* ib;
	IDirect3DVertexDeclaration9* decl;
	IDirect3DBaseTexture9* texture[MaxModTextures];
	IDirect3DPixelShader9* pixelShader;

	NativeModData() {
		memset(this,0,sizeof(NativeModData));
	}

	static int hashCode(int vertCount, int primCount) {
		//https://en.wikipedia.org/wiki/Pairing_function#Cantor_pairing_function
		return ( (vertCount + primCount) * (vertCount + primCount + 1) / 2 ) + primCount;
	}
};

// Interface used by the rest of the native code; all access to managed code must go through here.
namespace Interop {
	int InitCLR(WCHAR* mmPath);
	int ReloadAssembly();
	// If this returns false, calling a callback will explode in your face.
	bool OK(); 
	const ManagedCallbacks& Callbacks();
	const ConfData& Conf();
};
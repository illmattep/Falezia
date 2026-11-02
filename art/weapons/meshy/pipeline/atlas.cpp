// atlas <in.bin> <out.bin> <resolution> <padding>
// in:  u32 meshCount; per mesh: u32 nv, u32 ni, f32 pos[nv*3], f32 nrm[nv*3], u32 idx[ni]
// out: u32 meshCount, u32 atlasW, u32 atlasH; per mesh: u32 nv, u32 ni, u32 xref[nv], f32 uv[nv*2] (0..1), u32 idx[ni]
#include "../xatlas/source/xatlas/xatlas.h"
#include <cstdio>
#include <cstdlib>
#include <vector>
#include <cstdint>
int main(int argc, char** argv) {
    FILE* f = fopen(argv[1], "rb"); uint32_t mc; fread(&mc, 4, 1, f);
    std::vector<std::vector<float>> P(mc), N(mc); std::vector<std::vector<uint32_t>> I(mc);
    xatlas::Atlas* atlas = xatlas::Create();
    for (uint32_t m = 0; m < mc; m++) {
        uint32_t nv, ni; fread(&nv, 4, 1, f); fread(&ni, 4, 1, f);
        P[m].resize(nv * 3); N[m].resize(nv * 3); I[m].resize(ni);
        fread(P[m].data(), 4, nv * 3, f); fread(N[m].data(), 4, nv * 3, f); fread(I[m].data(), 4, ni, f);
        xatlas::MeshDecl d; d.vertexCount = nv; d.vertexPositionData = P[m].data(); d.vertexPositionStride = 12;
        d.vertexNormalData = N[m].data(); d.vertexNormalStride = 12;
        d.indexCount = ni; d.indexData = I[m].data(); d.indexFormat = xatlas::IndexFormat::UInt32;
        if (xatlas::AddMesh(atlas, d) != xatlas::AddMeshError::Success) { fprintf(stderr, "addmesh %u failed\n", m); return 1; }
    }
    fclose(f);
    xatlas::ChartOptions co; co.maxIterations = 2;
    xatlas::PackOptions po; po.resolution = atoi(argv[3]); po.padding = atoi(argv[4]); po.bilinear = true; po.blockAlign = false; po.bruteForce = argc > 5 && atoi(argv[5]) != 0; po.texelsPerUnit = argc > 6 ? (float)atof(argv[6]) : 0.0f;
    xatlas::Generate(atlas, co, po);
    fprintf(stderr, "atlas %ux%u charts %u count %u util %.2f\n", atlas->width, atlas->height, atlas->chartCount, atlas->atlasCount, atlas->utilization[0]);
    FILE* o = fopen(argv[2], "wb"); fwrite(&mc, 4, 1, o); fwrite(&atlas->width, 4, 1, o); fwrite(&atlas->height, 4, 1, o);
    for (uint32_t m = 0; m < mc; m++) {
        const xatlas::Mesh& me = atlas->meshes[m];
        fwrite(&me.vertexCount, 4, 1, o); fwrite(&me.indexCount, 4, 1, o);
        for (uint32_t v = 0; v < me.vertexCount; v++) fwrite(&me.vertexArray[v].xref, 4, 1, o);
        for (uint32_t v = 0; v < me.vertexCount; v++) { float uv[2] = { me.vertexArray[v].uv[0] / atlas->width, me.vertexArray[v].uv[1] / atlas->height }; fwrite(uv, 4, 2, o); }
        fwrite(me.indexArray, 4, me.indexCount, o);
    }
    fclose(o); xatlas::Destroy(atlas); return 0;
}

import sys, numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components
from views import mesh
def components(P,I):
    q=np.round(P/1e-5).astype(np.int64)
    _,weld=np.unique(q,axis=0,return_inverse=True); weld=weld.ravel()
    F=weld[I]; n=weld.max()+1
    r=np.concatenate([F[:,0],F[:,1]]); c=np.concatenate([F[:,1],F[:,2]])
    g=coo_matrix((np.ones(len(r)),(r,c)),shape=(n,n))
    k,lab=connected_components(g,directed=False)
    return k, lab[F[:,0]]
if __name__=='__main__':
    for name in sys.argv[1:]:
        js,b,P,UV,I,tex=mesh(f'src/{name}.glb')
        k,fl=components(P,I)
        cnt=np.bincount(fl); o=np.argsort(-cnt)
        print(name,'components',k)
        for c in o[:8]:
            m=fl==c; V=P[I[m].ravel()]
            print('   tris %7d  x[%.2f,%.2f] y[%.2f,%.2f] z[%.2f,%.2f]'%(cnt[c],*np.ravel(list(zip(V.min(0),V.max(0))))))

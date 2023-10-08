import numpy as np
from matplotlib import pyplot as plt



def solveCorner(p1, p2, p3):
    A = np.array([p1[0:3], p2[0:3], p3[0:3]])
    b = np.array([-p1[3], -p2[3], -p3[3]])
    x = np.linalg.solve(A, b)
    return x


'''
Right = 0,
Left = 1,
Bottom = 2,
Top = 3,
Back = 4,
Front = 5
'''



#Frustum equations
planes = [
[-0.9504458,-0.25857374,0.17260467,-3.7108877],
[-0.6527258,-0.25857374,-0.71210146,-1.6356567],
[-0.8368346,0.4694715,-0.2816103,-7.8071785],
[-0.44495285,-0.88294756,-0.14973482,3.5324435],
[0.9063602,0.29237187,0.30500805,1000.044],
[-0.9063606,-0.29237175,-0.30500707,-12.146753]
]

#Test AABB
aabb=[[-1424.6638, 493.0472, -163.3865], [-1401.6799, 758.5531, 85.7051]]

#Camera Pos
cp = [-5.670611, 6.7842116, 0.43740782]

#Camera Front
cf = [-0.90636057, -0.2923717, -0.30500707]



points = []
points.append(solveCorner(planes[0], planes[2], planes[4])) #Left, Bottom, Back
points.append(solveCorner(planes[0], planes[2], planes[5])) #Left, Bottom, Front
points.append(solveCorner(planes[0], planes[3], planes[4])) #Left, Top, Back
points.append(solveCorner(planes[0], planes[3], planes[5])) #Left, Top, Front
points.append(solveCorner(planes[1], planes[2], planes[4])) #Right, Bottom, Back
points.append(solveCorner(planes[1], planes[2], planes[5])) #Right, Bottom, Front
points.append(solveCorner(planes[1], planes[3], planes[4])) #Right, Top, Back
points.append(solveCorner(planes[1], planes[3], planes[5])) #Right, Top, Front

for p in points:
    print(p)

#Plot Points

fig = plt.figure()
ax = fig.add_subplot(projection='3d')

#Plot frustum points
ax.scatter([x[0] for x in points], [x[2] for x in points], [x[1] for x in points], label="FRUSTUM")

#Plot frustum main guiding lines
ax.plot([points[0][0], points[1][0]], [points[0][2], points[1][2]], [points[0][1], points[1][1]], label="LEFT_BOTTOM")
ax.plot([points[2][0], points[3][0]], [points[2][2], points[3][2]], [points[2][1], points[3][1]], label="LEFT_TOP")
ax.plot([points[4][0], points[5][0]], [points[4][2], points[5][2]], [points[4][1], points[5][1]], label="RIGHT_BOTTOM")
ax.plot([points[6][0], points[7][0]], [points[6][2], points[7][2]], [points[6][1], points[7][1]], label="RIGHT_TOP")

ax.plot([cp[0], cp[0] + 1000.0 * cf[0]], [cp[2], cp[2] + 1000.0 * cf[2]], [cp[1], cp[1] + 1000.0 * cf[1]], label="CAMERA POS")
ax.scatter([aabb[0][0], aabb[0][0], aabb[0][0], aabb[0][0], aabb[1][0], aabb[1][0], aabb[1][0], aabb[1][0]],
           [aabb[0][2], aabb[0][2], aabb[1][2], aabb[1][2], aabb[0][2], aabb[0][2], aabb[1][2], aabb[1][2]],
           [aabb[0][1], aabb[1][1], aabb[0][1], aabb[1][1], aabb[0][1], aabb[1][1], aabb[0][1], aabb[1][1]],
           label="AABB",color="green")

plt.legend()
plt.show()



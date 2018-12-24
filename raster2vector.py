import itertools, math
from PIL import Image
from svgwrite import Drawing, rgb, solidcolor

def dist(a, b):
	return math.sqrt((a[0] - b[0]) ** 2 + (a[1] - b[1]) ** 2)

width = height = 1000

print 'Loading'
edges = [-1] * (width * height)
for line in file('Converter/log'):
	x, y, depth, rdist = line[:-1].split(' ')
	x, y = int(x), int(y)
	depth = float(depth)
	#rdist = int(rdist)

	edges[y * width + x] = depth

patches = []
def flood(x, y):
	locs = []
	patches.append(locs)
	queue = [(edges[y * width + x], x, y)] # seed prev depth

	while len(queue):
		pdepth, x, y = queue.pop(0)
		i = y * width + x
		depth = edges[i]
		if x < 0 or x >= width or y < 0 or y >= height or depth == -1 or abs(pdepth - depth) > 1:
			continue

		locs.append((x, y))
		edges[i] = -1
		queue.append((depth, x - 1, y - 1))
		queue.append((depth, x    , y - 1))
		queue.append((depth, x + 1, y - 1))
		queue.append((depth, x - 1, y    ))
		queue.append((depth, x + 1, y    ))
		queue.append((depth, x - 1, y + 1))
		queue.append((depth, x    , y + 1))
		queue.append((depth, x + 1, y + 1))

print 'Flooding'
i = 0
for y in xrange(width):
	for x in xrange(height):
		if edges[i] != -1:
			flood(x, y)
		i += 1

print 'Culling tiny patches'
patches = [patch for patch in patches if len(patch) > 8]

"""
im = Image.new('1', (width, height))
pixels = [0] * (width * height)
for patch in patches:
	for x, y in patch:
		pixels[y * width + x] = 1
im.putdata(pixels)
im.save('test.png')
"""

nopt = (
	(-1, -1), 
	(0, -1), 
	(1, -1), 
	(-1, 0), 
	(1, 0), 
	(-1, 1), 
	(0, 1), 
	(1, 1)
)

def trace(patch):
	points = []
	pixels = [0] * (width * height)
	for x, y in patch:
		pixels[y * width + x] = 1

	def check(x, y):
		if x < 0 or x >= width or y < 0 or y >= height: return 0
		return pixels[y * width + x]

	def erase(x, y):
		if x < 0 or x >= width or y < 0 or y >= height: return
		pixels[y * width + x] = 0

	def count(x, y):
		sum = 0
		for i in xrange(-1, 2):
			for j in xrange(-1, 2):
				sum += check(x + i, y + j)
		return sum
	
	print 'Tracing patch of length', len(patch)
	lines = []
	queue = [(patch[0], None)]
	while len(queue):
		(x, y), prev = queue.pop(0)
		if count(x, y) == 0:
			continue
		if prev is not None:
			lines.append((prev, (x, y)))
		erase(x, y)
		for dx, dy in nopt:
			erase(x + dx, y + dy)

		opts = []
		for dx, dy in nopt:
			n = count(x + dx, y + dy)
			if n != 0:
				opts.append((x + dx, y + dy, n))
		opts = sorted(opts, key=lambda v: v[1], reverse=True)
		for nx, ny, _ in opts:
			queue.append(((nx, ny), (x, y)))

	"""
	origLineCount = len(lines)
	print 'Simplifying', origLineCount, 'lines'
	changed = True
	while changed:
		print 'Iteration (%i lines)' % len(lines)
		changed = False
		remove = set()
		nlines = []
		for pair in itertools.combinations(lines, 2):
			(a, b), (c, d) = pair
			abSlope = float(a[1] - b[1]) / (a[0] - b[0]) if a[0] != b[0] else None
			if not (a == c or a == d or b == c or b == d):
				continue
			cdSlope = float(c[1] - d[1]) / (c[0] - d[0]) if c[0] != d[0] else None
			comb = None
			if (b == c or d == a) and abSlope == cdSlope:
				comb = (a, d) if b == c else (b, c)
			elif (a == c or b == d) and ((abSlope is None and cdSlope is None) or (abSlope is not None and -abSlope == cdSlope)):
				comb = (b, d) if a == c else (a, c)
			if comb is None:
				continue
			nlines.append(comb)
			remove.add((a, b))
			remove.add((c, d))
			#print a, b, c, d, comb
			#dist = math.sqrt((comb[0][0] - comb[1][0]) ** 2 + (comb[0][1] - comb[1][1]) ** 2)
			#print 'Replaced with length', dist
			changed = True
		if changed:
			for elem in remove:
				lines.remove(elem)
			lines += nlines
	print 'Simplified to', len(lines), 'from', origLineCount"""
	return lines

patchSegments = map(trace, patches)

def sign(x):
	if x >= 0:
		return 1
	return -1

def optimizePath(path):
	if len(path) < 3:
		return path
	last = path[0]
	npath = []
	for i in xrange(1, len(path)):
		b, c = path[i - 1], path[i]
		if b == last:
			continue
		db = b[0] - last[0], b[1] - last[1]
		dc = c[0] - b[0], c[1] - b[1]
		if db[0] == 0 and dc[0] == 0 and sign(db[1]) == sign(dc[1]):
			last = c
		elif db[0] != 0 and dc[0] != 0 and db[1] / float(db[0]) == dc[1] / float(dc[0]):
			last = c
		else:
			npath.append(last)
			last = b

	npath.append(last)

	return npath

def pathify(lines):
	paths = []
	for a, b in lines:
		found = False
		for path in paths:
			aIn, bIn = a in path, b in path
			if aIn and bIn:
				continue
			end = path[-1]
			if end == a or end == b:
				path.append(a if end == b else b)
				found = True
				break
			start = path[0]
			if start == a or start == b:
				path.insert(0, a if start == b else b)
				found = True
				break
		if not found:
			paths.append([a, b])
	return paths#map(optimizePath, paths)

patchPaths = map(pathify, patchSegments)

allPaths = reduce(lambda a, x: a + x, patchPaths)

def minimizeTravel(paths):
	last = paths[0][-1]
	npaths = [paths[0]]
	remaining = paths[1:]

	while len(remaining):
		closest = (100000000, None, False) # distance, path, needReverse
		for elem in remaining:
			sd = dist(last, elem[0])
			ed = dist(last, elem[-1])
			if sd <= ed and sd < closest[0]:
				closest = (sd, elem, False)
				if sd == 0:
					break
			elif ed < sd and ed < closest[0]:
				closest = (ed, elem, True)
				if ed == 0:
					break
		assert closest[1] is not None
		cpath = closest[1]
		remaining.remove(cpath)
		if closest[2]:
			cpath = cpath[::-1]
		last = cpath[-1]
		npaths.append(cpath)

	return npaths

def calcTravel(paths):
	last = paths[0][-1]
	tdist = 0
	
	for path in paths[1:]:
		tdist += dist(last, path[0])
		last = path[-1]

	return tdist

print 'Current travel:', calcTravel(allPaths)
print 'Minimizing travel'
allPaths = minimizeTravel(allPaths)
print 'New travel:', calcTravel(allPaths)

dwg = Drawing('test.svg', profile='tiny')
print 'Building SVG'
for path in allPaths:
	#print '\tPath of', len(path), 'elements'
	spath = dwg.path(stroke='black')
	spath.stroke(color='black', width=1)
	spath.fill(color='red', opacity=0)
	spath.push('M %.1f %.1f' % (path[0][0] / 2.0, path[0][1] / 2.0))
	for elem in path[1:]:
		spath.push('L %.1f %.1f' % (elem[0] / 2.0, elem[1] / 2.0))
	dwg.add(spath)
	#dwg.add(dwg.line(a, b, stroke='black'))
print 'Saving'
dwg.save()

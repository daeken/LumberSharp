import itertools, math
from PIL import Image
from svgwrite import Drawing, rgb

width = height = 1000

print 'Loading'
edges = [-1] * (width * height)
for line in file('Converter/log'):
	x, y, depth, dist = line[:-1].split(' ')
	x, y = int(x), int(y)
	depth = float(depth)
	#dist = int(dist)

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
			lines.append(((x, y), prev))
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
	print 'Simplified to', len(lines), 'from', origLineCount
	return lines

patchSegments = map(trace, patches)

dwg = Drawing('test.svg', profile='tiny')
print 'Building SVG'
for segments in patchSegments:
	print 'Patch of', len(segments), 'lines'
	for a, b in segments:
		dwg.add(dwg.line(a, b, stroke='black'))
print 'Saving'
dwg.save()

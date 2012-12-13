import os
import math

def average(s): 
	if not s: return 0
	return sum(s) * 1.0 / len(s)
def variance(s): 
	avg = average(s)
	return map(lambda x: (x - avg)**2, s)
def stddev(s): return math.sqrt(average(variance(s)))

def getscores(fitness,count):
	fitness *= count**2
	fitness -= count**2
	if fitness < 0:
		fitness = -fitness
		awin = False
	else: awin = True
	fitness = math.sqrt(fitness)
	ascore = 0
	escore = 0
	if awin: ascore = fitness
	else: escore = fitness
	return ascore, escore
	
def processFile(fn, count, games = 10):
	scores = []
	f = open(fn, 'r')
	wins = 0
	losses = 0
	for i in range(games):
		line = f.readline().rstrip().split(',')
		if line[0] == '': continue
		score = float(line[1])
		if score > 1:
			wins += 1
		else:
			losses += 1
		scores += [score]
	avg = average(scores)
	std = stddev(scores)
	print "%s: Mean: %2.2f, Std Dev: %2.2f" % (fn, avg, std)
	print "Wins: %i, Losses: %i" % (wins, losses)
	print getscores(avg, count)

# filenames = os.listdir('.')
processFile('gw2v2_squad_human.csv', 4)
processFile('hetero20v20_individual_human.csv', 20)
processFile('mf3v3_multiunit_human.csv', 3)
processFile('hetero20v20_individual_builtin.csv', 20, 2000)
processFile('hetero20v20_individual_skynet.csv', 20, 2000)
processFile('hetero20v20_individual+_skynet.csv', 20, 2000)
# for fn in filenames:
	# if fn[-4:] != '.csv': continue
	# processFile(fn)
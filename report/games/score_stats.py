import os
import math
import numpy as np
from scipy.stats import ttest_1samp, wilcoxon, ttest_ind, mannwhitneyu

def average(s): 
	if not s: return 0
	return sum(s) * 1.0 / len(s)
def variance(s): 
	avg = average(s)
	return map(lambda x: (x - avg)**2, s)
def stddev(s): return math.sqrt(average(variance(s)))

def getWinsLosses(fitness,count):
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

def getScores(fn, games = 10):
	scores = []
	f = open(fn, 'r')
	for i in range(games):
		line = f.readline().rstrip().split(',')
		if line[0] == '': continue
		score = float(line[1])
		scores += [score]
	f.close()
	return scores
	
def processFile(fn, count, games = 10):
	wins = 0
	losses = 0
	scores = getScores(fn, games)
	for s in scores:
		if s > 1:
			wins += 1
		else:
			losses += 1
	avg = average(scores)
	std = stddev(scores)
	print "%s: Mean: %2.2f, Std Dev: %2.2f" % (fn, avg, std)
	print "Wins: %i, Losses: %i" % (wins, losses)
	print getWinsLosses(avg, count)
	
def ttest(fn1,games1,fn2,games2):
	group1 = getScores(fn1,games1)
	group2 = getScores(fn2,games2)
	t_statistic, p_value = ttest_ind(group1, group2)
	print t_statistic, p_value

# filenames = os.listdir('.')
processFile('gw2v2_squad_human.csv', 4)
processFile('hetero20v20_individual_human.csv', 20)
processFile('mf3v3_multiunit_human.csv', 3)
processFile('hetero20v20_individual_builtin.csv', 20, 2000)
processFile('hetero20v20_individual_skynet.csv', 20, 2000)
processFile('hetero20v20_individual+_skynet.csv', 20, 1000)
processFile('mf3v3_multiunit_builtin.csv', 3, 2000)
processFile('mfgw20v20_squad_builtin.csv', 3, 2000)
processFile('hetero20v20_individual.untrained_skynet.csv', 3, 2000)
ttest('hetero20v20_individual_skynet.csv', 2000, 'hetero20v20_individual+_skynet.csv', 1000)
# for fn in filenames:
	# if fn[-4:] != '.csv': continue
	# processFile(fn)
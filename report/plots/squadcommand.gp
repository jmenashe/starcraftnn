set terminal pdf
source = "squadcommand"
set output source.".pdf"
set xlabel "Generation";
set ylabel "Normalized Fitness";
set title "Squad Commander Population Fitness Over Time";
set datafile separator ',';
set key outside right center;
f(x) = a + b*log(x)
g(x) = .569028 + .0908996 * log(x)
fit f(x) source.".csv" u 12:2 via a, b
plot \
  f(x) title 'Population Trend' lw 10, \
  source.".csv" u 12:1 title 'Champion' with lines, \
  source.".csv" u 12:2 title 'Population Mean';
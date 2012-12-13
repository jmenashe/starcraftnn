load "fonts.gp"
set terminal pdf
source = "multiunit"
set output source.".pdf"
set xlabel "Generation";
set ylabel "Normalized Fitness";
set title "Multi-Unit Controller Population Fitness Over Time";
set datafile separator ',';
set key inside right bottom;
f(x) = a + b*log(x)
fit f(x) source.".csv" u 12:2 via a, b
plot \
  f(x) title 'Population Trend' lw 10, \
  source.".csv" u 12:1 title 'Champion' with lines, \
  source.".csv" u 12:2 title 'Population Mean';
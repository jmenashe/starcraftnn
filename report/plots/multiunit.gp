set terminal pdf
set output "multiunit.pdf"
set xlabel "Generation";
set ylabel "Normalized Fitness";
set title "Population Fitness Over Time";
set datafile separator ',';
set key outside right center;
f(x) = m*x + c
fit f(x) "multiunit.csv" u 12:2 via m, c
plot \
  f(x) title 'Population Trend' lw 10, \
  "multiunit.csv" u 12:1 title 'Champion' with lines, \
  "multiunit.csv" u 12:2 title 'Population Mean';
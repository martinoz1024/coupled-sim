%% Visualizee GapAcceptance
% This script vizualizes the gap acceptance data 
% Author: Johnson Mok
% Last Updated: 21-01-2021

function visualizeGapAcceptance(data,v,pasz)
[ND_Y, ND_NY, D_Y, D_NY] = getOrganizedDY(data);
[vND_Y, vND_NY, vD_Y, vD_NY] = getOrganizedDY(v);
[paszND_Y, paszND_NY, paszD_Y, paszD_NY] = getOrganizedDY(pasz);

% Summing and smoothing gap acceptance values per variable combination
sum_ND_Y = calcSumGapAcceptance(ND_Y);
sum_ND_NY = calcSumGapAcceptance(ND_NY);
sum_D_Y = calcSumGapAcceptance(D_Y);
sum_D_NY = calcSumGapAcceptance(D_NY);

smooth_ND_Y = smoothData(sum_ND_Y,11);
smooth_ND_NY = smoothData(sum_ND_NY,11);
smooth_D_Y = smoothData(sum_D_Y,11);
smooth_D_NY = smoothData(sum_D_NY,11);

% mean sum AV velocity
SvND_Y = abs(calcMeanGroup(vND_Y));
SvND_NY = abs(calcMeanGroup(vND_NY));
SvD_Y = abs(calcMeanGroup(vD_Y));
SvD_NY = abs(calcMeanGroup(vD_NY));

% mean sum AV Posz
paszND_Y = calcMeanGroup(paszND_Y);
paszND_NY = calcMeanGroup(paszND_NY);
paszD_Y = calcMeanGroup(paszD_Y);
paszD_NY = calcMeanGroup(paszD_NY);

% Pedestrian distance from the vehicle

% Decision certainty
[DC_ND_Y, DC_ND_NY, DC_D_Y, DC_D_NY, totalMeanChange] = calcDecisionCertainty(ND_Y, ND_NY, D_Y, D_NY);

% Visualization
visSumGapAcceptance(smooth_ND_Y,smooth_ND_NY,smooth_D_Y,smooth_D_NY);
visGapAcceptVSrbv(sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY,SvND_Y, SvND_NY, SvD_Y, SvD_NY);
visGapAcceptVSpasz(sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY,paszND_Y, paszND_NY, paszD_Y, paszD_NY);
visGapAcceptVSposped(sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY,paszND_Y, paszND_NY, paszD_Y, paszD_NY)
end

%% Helper functions
function [ND_Y, ND_NY, D_Y, D_NY] = getOrganizedDY(data)
field = fieldnames(data.Data_ED_0.HostFixedTimeLog);
% ND_Y: ED 0, 4, 8
ND_Y(:,1) = data.Data_ED_0.HostFixedTimeLog.(field{:});
ND_Y(:,2) = data.Data_ED_4.HostFixedTimeLog.(field{:});
ND_Y(:,3) = data.Data_ED_8.HostFixedTimeLog.(field{:});

% ND_NY: ED 1, 5, 9
ND_NY(:,1) = data.Data_ED_1.HostFixedTimeLog.(field{:});
ND_NY(:,2) = data.Data_ED_5.HostFixedTimeLog.(field{:});
ND_NY(:,3) = data.Data_ED_9.HostFixedTimeLog.(field{:});

% D_Y: ED 2, 6, 10
D_Y(:,1) = data.Data_ED_2.HostFixedTimeLog.(field{:});
D_Y(:,2) = data.Data_ED_6.HostFixedTimeLog.(field{:});
D_Y(:,3) = data.Data_ED_10.HostFixedTimeLog.(field{:});

% D_NY: ED 1, 5, 9
D_NY(:,1) = data.Data_ED_3.HostFixedTimeLog.(field{:});
D_NY(:,2) = data.Data_ED_7.HostFixedTimeLog.(field{:});
D_NY(:,3) = data.Data_ED_11.HostFixedTimeLog.(field{:});
end      
function out = calcSumGapAcceptance(data)
[max_size, ~] = max(cellfun('size', data, 1));
x = ceil((max(max_size))/50)*50;
out = zeros(x,3);
for j=1:size(data,2)
    for i=1:length(data)
        temp = data{i,j};
        out(1:length(temp),j) = out(1:length(temp),j) + temp;
    end
end
out = out*100/length(data);
end
function out = smoothData(data,factor) 
% factor = Number of data points for calculating the smoothed value|Default = 5
out = zeros(size(data));
for i = 1:size(data,2)
    out(:,i) = smooth(data(:,i),factor);
end
end
function out = calcMeanGroup(data)
% fill up the smaller arrays with the last known velocity, such that all
% arrrays have the same length.
[max_size, ~] = max(cellfun('size', data, 1));
x = ceil((max(max_size))/50)*50;
out = zeros(x,3);
for j=1:size(data,2)
    for i=1:length(data)
        temp = data{i,j};
        out(1:length(temp),j) = out(1:length(temp),j) + temp;
        for di = length(temp)+1:x
            out(di,j) = out(di,j) + temp(end);
        end
    end
end
out = out./length(data);
end
function [DC_ND_Y, DC_ND_NY, DC_D_Y, DC_D_NY, totalMeanChange] = calcDecisionCertainty(ND_Y, ND_NY, D_Y, D_NY)
Input = {ND_Y, ND_NY, D_Y, D_NY};
out = zeros(1,3);
for i = 1:length(Input)
    data = Input{i};
    changes = zeros(size(data));
    for col = 1:size(data,2)
        for row = 1:size(data,1)
            changes(row, col) = sum(diff(data{row,col})~=0);
        end
    end
    out(i,:) = mean(changes);
end
DC_ND_Y  = out(1,:);
DC_ND_NY = out(2,:);
DC_D_Y   = out(3,:);
DC_D_NY  = out(4,:);
totalMeanChange = mean(out);
end

function visSumGapAcceptance(sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY)
strMap = {'Baseline','Mapping 1','Mapping 2'};
dt = 0.0167;
data = {sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY}; 
titlestr = {'Gap Acceptance - No Distraction - Yielding';'Gap Acceptance - No Distraction - No yielding';...
    'Gap Acceptance - Distraction - Yielding'; 'Gap Acceptance - Distraction - No yielding'};
figure;
for i = 1:length(data)
    subplot(2,2,i)
    hold on;
    x = (1:length(data{i}))*dt;
    plot(x,data{i},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('gap acceptance in [%]'); 
    ylim([-0.5 105]);
    legend(strMap); title(titlestr{i});
end
end
function visGapAcceptVSrbv(sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY, SvND_Y, SvND_NY, SvD_Y, SvD_NY)
strMap = {'Baseline','Mapping 1','Mapping 2'};
dt = 0.0167;
data = {sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY}; 
v = {SvND_Y, SvND_NY, SvD_Y, SvD_NY};
gapstr = 'Gap Acceptance ';
velstr = 'AV velocity ';
titlestr = {'- No Distraction - Yielding';'- No Distraction - No yielding';...
    '- Distraction - Yielding'; '- Distraction - No yielding'};
for i = 1:2
    figure
    subplot(2,2,1)
    x = (1:length(data{i}))*dt;
    plot(x,data{i},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('gap acceptance in [%]'); 
    ylim([-0.5 105]);
    legend(strMap); title(join([gapstr,titlestr{i}]));
    
    subplot(2,2,2)
    x = (1:length(data{i+2}))*dt;
    plot(x,data{i+2},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('gap acceptance in [%]'); 
    ylim([-0.5 105]);
    legend(strMap); title(join([gapstr,titlestr{i+2}]));
    
    subplot(2,2,3)
    xv = (1:length(v{i}))*dt;
    plot(xv,v{i},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('AV velocity in [m/s]');
    ylim([-0.5 30.5]);
    legend(strMap); title(join([velstr,titlestr{i}]));
    
    subplot(2,2,4)
    xv = (1:length(v{i+2}))*dt;
    plot(xv,v{i+2},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('AV velocity in [m/s]');
    ylim([-0.5 30.5]);
    legend(strMap); title(join([velstr,titlestr{i+2}]));
end
end
function visGapAcceptVSpasz(sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY, paszND_Y, paszND_NY, paszD_Y, paszD_NY)
strMap = {'Baseline','Mapping 1','Mapping 2'};
dt = 0.0167;
data = {sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY}; 
v = {paszND_Y, paszND_NY, paszD_Y, paszD_NY};
gapstr = 'Gap Acceptance ';
velstr = 'AV z-position ';
titlestr = {'- No Distraction - Yielding';'- No Distraction - No yielding';...
    '- Distraction - Yielding'; '- Distraction - No yielding'};
for i = 1:2
    figure
    subplot(2,2,1)
    x = (1:length(data{i}))*dt;
    plot(x,data{i},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('gap acceptance in [%]'); 
    ylim([-0.5 105]);
    legend(strMap); title(join([gapstr,titlestr{i}]));
    
    subplot(2,2,2)
    x = (1:length(data{i+2}))*dt;
    plot(x,data{i+2},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('gap acceptance in [%]'); 
    ylim([-0.5 105]);
    legend(strMap); title(join([gapstr,titlestr{i+2}]));
    
    subplot(2,2,3)
    xv = (1:length(v{i}))*dt;
    plot(xv,v{i},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('AV z-position in [m]');
    yline(17.19, '-.b','Pedestrian pos','LineWidth',2);
%     ylim([-0.5 30.5]);
    legend(strMap); title(join([velstr,titlestr{i}]));
    
    subplot(2,2,4)
    xv = (1:length(v{i+2}))*dt;
    plot(xv,v{i+2},'LineWidth',2);
    grid on; xlabel('time in [s]'), ylabel('AV z-position in [m]');
    yline(17.19, '-.b','Pedestrian pos','LineWidth',2);
%     ylim([-0.5 30.5]);
    legend(strMap); title(join([velstr,titlestr{i+2}]));
end
end
function visGapAcceptVSposped(sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY,paszND_Y, paszND_NY, paszD_Y, paszD_NY)
strMap = {'Baseline','Mapping 1','Mapping 2'};
gapstr = 'Gap Acceptance ';
titlestr = {'- No Distraction - Yielding';'- No Distraction - No yielding';...
    '- Distraction - Yielding'; '- Distraction - No yielding'};
data = {sum_ND_Y,sum_ND_NY,sum_D_Y,sum_D_NY}; 
v = {paszND_Y, paszND_NY, paszD_Y, paszD_NY};
posped = 17.19;

figure;
for i = 1:length(data)
    subplot(2,2,i);
    x = v{i}-posped;
    plot(x,data{i},'LineWidth',2);
    set(gca, 'XDir','reverse');
    xlabel('Distance till pedestrian in [m]'); ylabel('gap acceptance in [%]');
    title(join([gapstr,titlestr{i}]));
    legend(strMap); grid on;
end
end

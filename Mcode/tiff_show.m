% TIFF图像读取与显示脚本
clc;
clear;
disp('TIFF图像读取与显示');

% 使用对话框选择TIFF文件
[filename, pathname] = uigetfile({'*.tif;*.tiff', 'TIFF Files (*.tif, *.tiff)'}, '选择TIFF文件');

% 如果用户取消，则退出
if isequal(filename, 0) || isequal(pathname, 0)
    disp('用户取消了操作');
    return;
end

% 完整路径
fullPath = fullfile(pathname, filename);

% 读取TIFF文件信息
info = imfinfo(fullPath);

% 显示图像信息
disp(['文件名: ', filename]);
disp(['图像尺寸: ', num2str(info(1).Width), ' x ', num2str(info(1).Height)]);
disp(['位深度: ', num2str(info(1).BitDepth)]);
disp(['颜色类型: ', info(1).ColorType]);
numFrames = length(info);
fprintf('帧数: %d\n', numFrames);

% 读取图像
img = imread(fullPath);

% 显示图像值范围
disp(['最小值: ', num2str(min(img(:)))]);
disp(['最大值: ', num2str(max(img(:)))]);
disp(['平均值: ', num2str(mean(double(img(:))))]);

% 创建图形显示
figure('Name', ['TIFF图像 - ', filename], 'NumberTitle', 'off');

% 显示原始图像
subplot(1, 2, 1);
imshow(img, []);  % [] 参数让MATLAB自动调整显示范围
title('TIFF图像');

% 显示直方图
subplot(1, 2, 2);
if isa(img, 'uint16')
    % 对于16位图像，显示缩放后的直方图
    imhist(uint8(mat2gray(img) * 255));
    title('直方图 (缩放至8位)');
else
    % 对于8位图像，直接显示直方图
    imhist(img);
    title('直方图');
end

% 保存数据到工作区变量，便于进一步分析
tiffData = img;
tiffInfo = info;
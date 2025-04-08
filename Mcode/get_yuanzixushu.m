clear;
close all;
clc;

% 读取第二帧
img = imread('_Unknown_1234_241220165218_02bf2810c47d45ad854950e0aa17fd66FusionView2.Tiff', 2);

% 判断矩阵维度
%if ndims(img) == 2
    %disp('第二帧是灰度图');
%elseif ndims(img) == 3
    %disp('第二帧是彩色图（RGB）');
%end

%MatToColorIndexLut(256) = 50; % 图像背景
%MatToColorIndexLut(255) = 50; % 图像背景
%MatToColorIndexLut(253) = 2; % 穿不透
%MatToColorIndexLut(254) =1; % 无法分辨

%test = imread('_Unknown_1234_241220165218_02bf2810c47d45ad854950e0aa17fd66FusionView2.Tiff',2);
%figure;
%imshow(test,[]);

material_num=uint16(imread('_Unknown_1234_241220165218_02bf2810c47d45ad854950e0aa17fd66FusionView2.Tiff',2));
figure,imshow(uint8(material_num));

data(:,1) = {'聚乙烯','水(液体)','碳酸氢钠','无水碳酸钠','碳化硅','铝','碳酸钙','氢氧化钙','氯化钾','高锰酸钾','钛六铝四钒','铁'}';
data(:,2) = {'C2H4','H2O','NaHCO3','Na2CO3','SiC','Al','CaCO3','Ca(OH)2','KCl','KMnO4','Ti6Al4V','Fe'}';
data(:,3)  = {'PE','HO','NH','NC','SC','Al','AO','AH','KL','KM','TV','Fe'}';
data(:,4) = {5.53 7.51 8.85 9.45 10 13 15.62 16.8 18.12 19.68 20 26 }';
yuanzixushu=[5.53, 7.51, 8.85, 9.45, 10 ,13, 15.62, 16.8, 18.12, 19.68 ,20 ,26];
%%%下面的这个灰度这句不一定对，到时候要换新的
huidu=[5,20,50,60,90,110,120,130,170,190,210,233];%%不同物质灰度值映射关系

cailiaozhi_num=zeros(size(material_num));
for i=1:size(material_num,1)
    for j=1:size(material_num,2)
        %%获取材料值
        material_num_temp=material_num(i,j);
        %%计算离他最近的两组标定材料
        left_quyu=max(find(material_num_temp>=huidu));
        right_quyu=min(find(material_num_temp<=huidu));
        %%计算两边系数
        %%先判定几种特殊情况。空气背景和穿不透
        if(material_num_temp==1||material_num_temp==2)
            yuanzixushu_temp=27;
        elseif(material_num_temp==50)
            yuanzixushu_temp=0;
        %%如果比最大的标定大或者比最小的标定小，就默认为对应的最大或者最小
        elseif(isempty(left_quyu))
            yuanzixushu_temp=5.53;
        elseif (isempty(right_quyu))
            yuanzixushu_temp=26;
        else      
            %%如果不是正好处于标定线上
            if(left_quyu==right_quyu)
                left_xishu=1;
                right_xishu=0;
            else
                left_xishu=(huidu(right_quyu)-material_num_temp)/(huidu(right_quyu)-huidu(left_quyu));
                right_xishu=(material_num_temp-huidu(left_quyu))/(huidu(right_quyu)-huidu(left_quyu));
            end
            %%根据对应材料的原子序数乘上对应系数比例
            yuanzixushu_temp=yuanzixushu(left_quyu)*left_xishu+yuanzixushu(right_quyu)*right_xishu;
        end
        %%获取原子序数
        cailiaozhi_num(i,j)=yuanzixushu_temp;
    end
end
test = uint8(cailiaozhi_num);
figure;
%imagesc(cailiaozhi_num);
imshow(test,[]);
%colorbar; % 添加颜色条来显示数值范围



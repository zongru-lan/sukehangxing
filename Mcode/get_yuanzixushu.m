clear;
close all;
clc;

% ��ȡ�ڶ�֡
img = imread('_Unknown_1234_241220165218_02bf2810c47d45ad854950e0aa17fd66FusionView2.Tiff', 2);

% �жϾ���ά��
%if ndims(img) == 2
    %disp('�ڶ�֡�ǻҶ�ͼ');
%elseif ndims(img) == 3
    %disp('�ڶ�֡�ǲ�ɫͼ��RGB��');
%end

%MatToColorIndexLut(256) = 50; % ͼ�񱳾�
%MatToColorIndexLut(255) = 50; % ͼ�񱳾�
%MatToColorIndexLut(253) = 2; % ����͸
%MatToColorIndexLut(254) =1; % �޷��ֱ�

%test = imread('_Unknown_1234_241220165218_02bf2810c47d45ad854950e0aa17fd66FusionView2.Tiff',2);
%figure;
%imshow(test,[]);

material_num=uint16(imread('_Unknown_1234_241220165218_02bf2810c47d45ad854950e0aa17fd66FusionView2.Tiff',2));
figure,imshow(uint8(material_num));

data(:,1) = {'����ϩ','ˮ(Һ��)','̼������','��ˮ̼����','̼����','��','̼���','��������','�Ȼ���','�������','�������ķ�','��'}';
data(:,2) = {'C2H4','H2O','NaHCO3','Na2CO3','SiC','Al','CaCO3','Ca(OH)2','KCl','KMnO4','Ti6Al4V','Fe'}';
data(:,3)  = {'PE','HO','NH','NC','SC','Al','AO','AH','KL','KM','TV','Fe'}';
data(:,4) = {5.53 7.51 8.85 9.45 10 13 15.62 16.8 18.12 19.68 20 26 }';
yuanzixushu=[5.53, 7.51, 8.85, 9.45, 10 ,13, 15.62, 16.8, 18.12, 19.68 ,20 ,26];
%%%���������Ҷ���䲻һ���ԣ���ʱ��Ҫ���µ�
huidu=[5,20,50,60,90,110,120,130,170,190,210,233];%%��ͬ���ʻҶ�ֵӳ���ϵ

cailiaozhi_num=zeros(size(material_num));
for i=1:size(material_num,1)
    for j=1:size(material_num,2)
        %%��ȡ����ֵ
        material_num_temp=material_num(i,j);
        %%�����������������궨����
        left_quyu=max(find(material_num_temp>=huidu));
        right_quyu=min(find(material_num_temp<=huidu));
        %%��������ϵ��
        %%���ж�����������������������ʹ���͸
        if(material_num_temp==1||material_num_temp==2)
            yuanzixushu_temp=27;
        elseif(material_num_temp==50)
            yuanzixushu_temp=0;
        %%��������ı궨����߱���С�ı궨С����Ĭ��Ϊ��Ӧ����������С
        elseif(isempty(left_quyu))
            yuanzixushu_temp=5.53;
        elseif (isempty(right_quyu))
            yuanzixushu_temp=26;
        else      
            %%����������ô��ڱ궨����
            if(left_quyu==right_quyu)
                left_xishu=1;
                right_xishu=0;
            else
                left_xishu=(huidu(right_quyu)-material_num_temp)/(huidu(right_quyu)-huidu(left_quyu));
                right_xishu=(material_num_temp-huidu(left_quyu))/(huidu(right_quyu)-huidu(left_quyu));
            end
            %%���ݶ�Ӧ���ϵ�ԭ���������϶�Ӧϵ������
            yuanzixushu_temp=yuanzixushu(left_quyu)*left_xishu+yuanzixushu(right_quyu)*right_xishu;
        end
        %%��ȡԭ������
        cailiaozhi_num(i,j)=yuanzixushu_temp;
    end
end
test = uint8(cailiaozhi_num);
figure;
%imagesc(cailiaozhi_num);
imshow(test,[]);
%colorbar; % �����ɫ������ʾ��ֵ��Χ



from typing import Dict, Tuple, Optional
from argparse import ArgumentParser
from pathlib import Path
from datetime import datetime, timedelta
import logging
import pickle
import xml.etree.ElementTree as ET
from filterpy.kalman import KalmanFilter
from filterpy.common import Q_discrete_white_noise
from tqdm import tqdm
import pandas as pd
import numpy as np
import time 
# Define subject types in the classroom
CHILD_SUBJECT = "Child"
TEACHER_SUBJECT = "Teacher"
LAB_MEMBER_SUBJECT = "Lab"
SUBJECT_TYPES = [CHILD_SUBJECT, TEACHER_SUBJECT, LAB_MEMBER_SUBJECT]
GAP_TOLERANCE = 60

#def load_mapping_file(path: Path, remove_lab: bool = True) -> pd.DataFrame:
def load_mapping_file(path: Path, remove_lab: bool = False) -> pd.DataFrame:
    """ Load mapping file

    The mapping file should contain six columns: Subject_ID, Roster, TYPE, Left Tag, Right Tag, and LENA DLP ID.
    The values in the column TYPE should be one of the :attr:SUBJECT_TYPES

    Args:
        path (Path): The filepath to the mapping file
        remove_lab (bool): If True, the lab members are removed from the data

    Returns:
        pd.DataFrame: the metadata of subjects in the classroom in a DataFrame indexed by subject ID
    """
    
    df=pd.read_csv(path, dtype=str, keep_default_na=False)
    
    data = dict()
    for _, row in pd.read_csv(path, dtype=str, keep_default_na=False).iterrows():
        subject_id = ""

        if 'Subject_ID' in df.columns: 
            subject_id=row["Subject_ID"]
        else:
            subject_id=row[0]

        lenaId=""
        if 'LENA' in df.columns:
            lenaId=row["LENA"]
        elif 'DLP' in df.columns:
            lenaId=row["DLP"]
        elif 'LENA DLP ID' in df.columns:
            lenaId=row["LENA DLP ID"]
        else:
            lenaId=row[8]
         
        leftTag=""
        rightTag=""

        if 'Left Tag' in df.columns:
            leftTag=row["Left Tag"]
        elif 'Left Ubi' in df.columns:
            leftTag=row["Left Ubi"]
        elif 'Left Tag Ubi' in df.columns:
            leftTag=row["Left Tag Ubi"]
        elif 'Left Tag (Ubi)' in df.columns:
            leftTag=row["Left Tag (Ubi)"]

        if 'Right Tag' in df.columns:
            rightTag=row["Right Tag"]
        elif 'Right Ubi' in df.columns:
            rightTag=row["Right Ubi"]
        elif 'Right Tag Ubi' in df.columns:
            rightTag=row["Right Tag Ubi"]
        elif 'Right Tag (Ubi)' in df.columns:
            rightTag=row["Right Tag (Ubi)"]

        #logging.warning("subject_id: "+subject_id)
        #logging.warning("lenaId: "+lenaId)
        #logging.warning("leftTag: "+leftTag)
        #logging.warning("rightTag: "+rightTag)
        
        if subject_id != "" and lenaId!="" and leftTag!="" and rightTag!="" :
            # Validate the integrity of subject types
            logging.warning("FIRST BLOCK "+subject_id)
            #if row["TYPE"] not in SUBJECT_TYPES:
            #    raise ValueError("Unknown subject type in the mapping file: {}".format(row["TYPE"]))

            if row["STATUS"] == "ABSENT" or row["STATUS"] == "NODATA":
                continue
            
            subjectType="Child"
            
            if 'TYPE' in df.columns and row["TYPE"]!="":
                logging.warning("HELLO TYPE0")
                subjectType=row["TYPE"]
            elif "_T" in subject_id:
                subjectType="Teacher"
            elif "_L" in subject_id and "_AM_" not in subject_id  and "_PM_" not in subject_id  and "_TRTL_" not in subject_id :
                subjectType="Lab"
            logging.warning("subjectType  "+subjectType)
                
            if remove_lab and subjectType == LAB_MEMBER_SUBJECT:
                continue
            
            logging.warning("SUBJECT BEING ADDED TO DATA "+subject_id)
            data[subject_id] = {
                "Name": subject_id,
                "Type": subjectType,
                "LeftTag": leftTag,
                "RightTag": rightTag,
                "LENA": int(lenaId)
            }
        elif 'Left Ubi' in df.columns and subject_id != "" and row["LENA"]!="" and row["Left Ubi"]!="" and row["Right Ubi"]!="" :
            # Validate the integrity of subject types
            logging.warning("SECOND BLOCK ")
            subjectType="Child"
             
            if 'TYPE' in df.columns and row["TYPE"]!="":
                logging.warning("HELLO TYPE")
                subjectType=row["TYPE"]
            elif "_T" in subject_id:
                subjectType="Teacher"
            elif "_L" in subject_id and "_AM_" not in subject_id  and "_PM_" not in subject_id :
                subjectType="Lab"

            if subjectType not in SUBJECT_TYPES:
                raise ValueError("Unknown subject type in the mapping file: {}".format(subjectType))

            if remove_lab and subjectType == LAB_MEMBER_SUBJECT:
                continue
            if row["STATUS"] == "ABSENT" or row["STATUS"] == "NODATA":
                continue
            
            data[subject_id] = {
                "Name": row[1],
                "Type": subjectType,
                "LeftTag": leftTag,
                "RightTag": rightTag,
                "LENA": int(lenaId)
            }
    dict_length = len(data)
    logging.warning("The length of data is:"+   str(dict_length))
    return pd.DataFrame.from_dict(data, orient="index")


def load_motion_data_file(path: Path, mapping: pd.DataFrame) -> Dict[str, pd.DataFrame]:
    """ Load motion data file in the Ubisense Location file format

    The motion data file has at least six columns.
    The second to sixth columns are tag ID, Time, X, Y, Z, respectively.

    Args:
        path (Path): The filepath to the motion data file
        mapping (pd.DataFrame): The classroom metadata generated by :func:load_mapping_file

    Returns:
        Dict[str, pd.DataFrame]: A Dict mapping tag ID to the corresponding classroom motion data
    """

    # Generate the mapper from raw tag ID to subject ID and position
    tag_id_mapper = {row["LeftTag"]: "{}L".format(sid) for sid, row in mapping.iterrows()}
    tag_id_mapper.update({row["RightTag"]: "{}R".format(sid) for sid, row in mapping.iterrows()})
    logging.warning("The length of tag_id_mapper is:"+   str(len(tag_id_mapper)))
    
    # Load motion data file
    data = pd.read_csv(path, names=["TagID", "Time", "X", "Y", "Z"], usecols=[1, 2, 3, 4, 5])
    data = data[data["TagID"].isin(tag_id_mapper)]
    data["TagID"] = data["TagID"].map(tag_id_mapper)
    data["Time"] = data["Time"].map(lambda x: datetime.fromisoformat(x))

    # Group data by TagID
    data = {tid: d.drop(columns="TagID") for tid, d in data.groupby("TagID")}
    logging.warning("The length of data is:")
    data_length = len(data)
    logging.warning("The length of data is:"+   str(data_length))
    return data


def pair_and_interpolate_motion_data(
        data: Dict[str, pd.DataFrame], t_delta: timedelta = timedelta(microseconds=100000)
) -> Dict[str, pd.DataFrame]:
    """ Pair and interpolate raw motion data from the file in the Ubisense Location format

    Args:
        data (Dict[str, pd.DataFrame]): The raw classroom motion data generated by :func:load_motion_data_file
        t_delta (timedelta): Time difference between adjacent data points after interpolation

    Returns:
        Dict[str, pd.DataFrame]: A Dict mapping subject ID to the corresponding classroom motion data
    """
    logging.warning('pair_and_interpolate_motion_data')
    logging.warning('data len '+ str(len(data)))
    subject_ids = list(sorted(set(tid[:-1] for tid in data)))
    logging.warning('subject_ids len '+ str(len(subject_ids)))
    rst = dict()
    for i, sid in enumerate(subject_ids, 1):
        if "{}L".format(sid) not in data or "{}R".format(sid) not in data:
            logging.warning("{} is dropped since only data from one tag are found".format(sid))
            continue  # Only data from one tag, drop the subject

        data_l = data["{}L".format(sid)]
        data_r = data["{}R".format(sid)]

        sdata = {c: list() for c in ["Time", "lx", "ly", "lz", "rx", "ry", "rz"]}
        data_l = data_l.sort_values("Time").reset_index(drop=True)
        data_r = data_r.sort_values("Time").reset_index(drop=True)

        # Determine the range of interpolation
        t_min = max(data_l["Time"].min(), data_r["Time"].min())
        t_max = min(data_l["Time"].max(), data_r["Time"].max())



        # Linear interpolate position at given frequency
        t = t_min + timedelta(microseconds=int(
            np.ceil(float(t_min.microsecond) / t_delta.total_seconds() / 1e6) * t_delta.total_seconds() * 1e6
        ) - t_min.microsecond)
        progress = tqdm(
            desc="Pair and interpolate motion data for {} ({}/{})".format(sid, i, len(subject_ids)),
            total=int((t_max - t) / t_delta + 1)
        )
             

        idx_l = data_l[data_l["Time"] >= t].first_valid_index()
        idx_r = data_r[data_r["Time"] >= t].first_valid_index()

        #logging.warning("the T  {} t_min {}  idx_l {} idx_r {}".format(t,t_min,idx_l,idx_r ))
        
        if idx_l is None or idx_r is None:
            logging.warning("{} are dropped since data from both tags do not overlap".format(sid))
            continue  # No overlapped data, drop the subject
        
        while t <= t_max:
            #logging.warning("TTTTTTT {} data_l[Time][idx_l] {} data_r[Time][idx_r] {}   ".format(t,data_l["Time"][idx_l],data_r["Time"][idx_r] ))
            
            while idx_l<=0 or data_l["Time"][idx_l] < t:
                idx_l += 1
                #logging.warning("LT {} data_l[Time][idx_l] {} data_r[Time][idx_r] {}   ".format(t,data_l["Time"][idx_l],data_r["Time"][idx_r] ))
            while idx_r<=0 or data_r["Time"][idx_r] < t:
                idx_r += 1
            #logging.warning("t {} t_delta {} idx_l {} idx_r {}  ".format(t,t_delta,idx_l,idx_r))
            
             
            #sdata["Time"].append(t)
            #if abs((data_l["Time"][idx_l] - data_r["Time"][idx_r]).total_seconds()) > GAP_TOLERANCE and (abs((data_l["Time"][idx_l] - data_l["Time"][idx_l-1]).total_seconds()) < GAP_TOLERANCE or abs((data_r["Time"][idx_r] - data_r["Time"][idx_r-1]).total_seconds()) < GAP_TOLERANCE or abs((data_r["Time"][idx_r] - data_l["Time"][idx_l-1]).total_seconds()) < GAP_TOLERANCE or abs((data_l["Time"][idx_l] - data_r["Time"][idx_r-1]).total_seconds()) < GAP_TOLERANCE):
                #logging.warning("REAL SECONDS>GAP {} {} {}  ".format(data_l["Time"][idx_l],data_r["Time"][idx_r],(data_l["Time"][idx_l] - data_r["Time"][idx_r-1]).total_seconds()))
                #time.sleep(20)
            #fix bug/corrected if abs((data_l["Time"][idx_l] - data_l["Time"][idx_l-1]).total_seconds()) < GAP_TOLERANCE or abs((data_r["Time"][idx_r] - data_r["Time"][idx_r-1]).total_seconds()) < GAP_TOLERANCE or abs((data_r["Time"][idx_r] - data_l["Time"][idx_l-1]).total_seconds()) < GAP_TOLERANCE or abs((data_l["Time"][idx_l] - data_r["Time"][idx_r-1]).total_seconds()) < GAP_TOLERANCE:
            #original wrong if abs((data_l["Time"][idx_l] - data_r["Time"][idx_r]).total_seconds()) < GAP_TOLERANCE:
            if abs((data_l["Time"][idx_l] - data_l["Time"][idx_l-1]).total_seconds()) < GAP_TOLERANCE or abs((data_r["Time"][idx_r] - data_r["Time"][idx_r-1]).total_seconds()) < GAP_TOLERANCE or abs((data_r["Time"][idx_r] - data_l["Time"][idx_l-1]).total_seconds()) < GAP_TOLERANCE or abs((data_l["Time"][idx_l] - data_r["Time"][idx_r-1]).total_seconds()) < GAP_TOLERANCE:
                sdata["Time"].append(t)
                w_l = (data_l["Time"][idx_l] - t) / (data_l["Time"][idx_l] - data_l["Time"][idx_l - 1])
                w_r = (data_r["Time"][idx_r] - t) / (data_r["Time"][idx_r] - data_r["Time"][idx_r - 1])
                sdata["lx"].append(data_l["X"][idx_l - 1] * (1 - w_l) + data_l["X"][idx_l] * w_l)
                sdata["ly"].append(data_l["Y"][idx_l - 1] * (1 - w_l) + data_l["Y"][idx_l] * w_l)
                sdata["lz"].append(data_l["Z"][idx_l - 1] * (1 - w_l) + data_l["Z"][idx_l] * w_l)
                sdata["rx"].append(data_r["X"][idx_r - 1] * (1 - w_r) + data_r["X"][idx_r] * w_r)
                sdata["ry"].append(data_r["Y"][idx_r - 1] * (1 - w_r) + data_r["Y"][idx_r] * w_r)
                sdata["rz"].append(data_r["Z"][idx_r - 1] * (1 - w_r) + data_r["Z"][idx_r] * w_r)
            #else:
            #    sdata["lx"].append(np.nan)
            #    sdata["ly"].append(np.nan)
            #    sdata["lz"].append(np.nan)
            #   sdata["rx"].append(np.nan)
            #    sdata["ry"].append(np.nan)
            #    sdata["rz"].append(np.nan)

            t += t_delta
            progress.update(1)

        rst[sid] = pd.DataFrame.from_dict(sdata)
        progress.close()
     

    return rst


def compute_subject_orientation(
        data: Dict[str, pd.DataFrame], lx_col: str, ly_col: str, rx_col: str, ry_col: str, o_col: str
) -> None:
    """ Compute subject orientation based on 2D positions of left and right tags

    The subject orientation is on x-y plane in the coordination system of left and right tags.
    The orientation ranges in [-pi, pi]
    Facing up (y+) is 0 degree, positive when facing right (x+), and negative when facing left (x-).

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        lx_col (str): The column name of lx
        ly_col (str): The column name of ly
        rx_col (str): The column name of rx
        ry_col (str): The column name of ry
        o_col (str): The column name to store the results
    """

    for sid, sdata in data.items():
        sdata[o_col] = np.arctan2(sdata[ly_col] - sdata[ry_col], sdata[rx_col] - sdata[lx_col])


def compute_tag_2d_distance(
        data: Dict[str, pd.DataFrame], lx_col: str, ly_col: str, rx_col: str, ry_col: str, o_col: str
) -> None:
    """ Compute the distance between left and right tags in 2D space

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        lx_col (str): The column name of lx
        ly_col (str): The column name of ly
        rx_col (str): The column name of rx
        ry_col (str): The column name of ry
        o_col (str): The column name to store the results
    """

    for sid, sdata in data.items():
        sdata[o_col] = np.sqrt(np.square(sdata[lx_col] - sdata[rx_col]) + np.square(sdata[ly_col] - sdata[ry_col]))


def compute_tag_center_position(
        data: Dict[str, pd.DataFrame], lx_col: str, ly_col: str, lz_col: str, rx_col: str, ry_col: str, rz_col: str,
        cx_col: str, cy_col: str, cz_col: str
) -> None:
    """ Compute the center position of left and right tags

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        lx_col (str): The column name of lx
        ly_col (str): The column name of ly
        lz_col (str): The column name of lz
        rx_col (str): The column name of rx
        ry_col (str): The column name of ry
        rz_col (str): The column name of rz
        cx_col (str): The column name to store the computed cx
        cy_col (str): The column name to store the computed cy
        cz_col (str): The column name to store the computed cz
    """

    for sid, sdata in data.items():
        sdata[cx_col] = (sdata[lx_col] + sdata[rx_col]) / 2
        sdata[cy_col] = (sdata[ly_col] + sdata[ry_col]) / 2
        sdata[cz_col] = (sdata[lz_col] + sdata[rz_col]) / 2


def compute_left_and_right_tag_positions(
        data: Dict[str, pd.DataFrame], cx_col: str, cy_col: str, o_col: str, d2d_col: str,
        lx_col: str, ly_col: str, rx_col: str, ry_col: str
) -> None:
    """ Compute the positions of both left and right tags based on center, orientation and distance between tags

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        cx_col (str): The column name of cx
        cy_col (str): The column name of cy
        o_col (str): The column name of subject orientation
        d2d_col (str): The column name of distance between left and right tags in 2D space
        lx_col (str): The column name to store the computed lx
        ly_col (str): The column name to store the computed ly
        rx_col (str): The column name to store the computed rx
        ry_col (str): The column name to store the computed ry
    """

    for sid, sdata in data.items():
        sdata[lx_col] = sdata[cx_col] - sdata[d2d_col] * np.cos(sdata[o_col]) / 2
        sdata[ly_col] = sdata[cy_col] + sdata[d2d_col] * np.sin(sdata[o_col]) / 2
        sdata[rx_col] = sdata[cx_col] + sdata[d2d_col] * np.cos(sdata[o_col]) / 2
        sdata[ry_col] = sdata[cy_col] - sdata[d2d_col] * np.sin(sdata[o_col]) / 2


def compute_tag_center_2d_position(
        data: Dict[str, pd.DataFrame], lx_col: str, ly_col: str, rx_col: str, ry_col: str, cx_col: str, cy_col: str
) -> None:
    """ Compute the center position of left and right tags

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        lx_col (str): The column name of lx
        ly_col (str): The column name of ly
        rx_col (str): The column name of rx
        ry_col (str): The column name of ry
        cx_col (str): The column name to store the computed cx
        cy_col (str): The column name to store the computed cy
    """

    for sid, sdata in data.items():
        sdata[cx_col] = (sdata[lx_col] + sdata[rx_col]) / 2
        sdata[cy_col] = (sdata[ly_col] + sdata[ry_col]) / 2


def _find_nearest_opposite_outlier(
        o_delta: np.ndarray, opp_outliers: np.ndarray, pos_target: int, outlier_range: int
) -> Tuple[Optional[int], Optional[float]]:
    """ Find the nearest outlier in the opposite direction

    Args:
        o_delta (np.ndarray): Array of subject orientation velocity (current)
        opp_outliers (np.ndarray): Indicator array of opposite outliers
        pos_target (int): The position of target outlier to cancel
        outlier_range (int): The search range for outlier removal

    Returns:
        int: The position of the nearest opposite outlier
        float: The amount to cancel
    """
    t_min, t_max = pos_target - outlier_range, pos_target + outlier_range + 1
    nearby_opp_spikes = np.argwhere(opp_outliers[t_min:t_max] == 1)
    if len(nearby_opp_spikes) > 0:
        nearby_opp_spikes = sorted(
            nearby_opp_spikes,
            key=lambda x: outlier_range - x if outlier_range - x > 0 else outlier_range - x + 0.5
        )
        pos_opp = pos_target - outlier_range + nearby_opp_spikes[0]
        amount = min(abs(o_delta[pos_target]), abs(o_delta[pos_opp]))
        return pos_opp, amount
    else:
        return None, None


def _find_nearest_opposite_suboutliers(
        o_delta: np.ndarray, opp_suboutliers: np.ndarray, pos_target: int, outlier_range: int
) -> Tuple[Optional[int], Optional[int], Optional[float]]:
    """ Find the first and second nearest outlier in the opposite direction

    Args:
        o_delta (np.ndarray): Array of subject orientation velocity (current)
        opp_suboutliers (np.ndarray): Indicator array of opposite suboutliers
        pos_target (int): The position of target outlier to cancel
        outlier_range (int): The search range for outlier removal

    Returns:
        int: The position of the nearest opposite suboutlier
        int: The position of the second nearest opposite suboutlier
        float: The amount to cancel
    """
    t_min, t_max = pos_target - outlier_range, pos_target + outlier_range + 1
    nearby_opp_suboutliers = np.argwhere(opp_suboutliers[t_min:t_max] == 1)
    if len(nearby_opp_suboutliers) > 1:
        nearby_opp_suboutliers = sorted(
            nearby_opp_suboutliers,
            key=lambda x: outlier_range - x if outlier_range - x > 0 else outlier_range - x + 0.5
        )
        pos_opp1 = pos_target - outlier_range + nearby_opp_suboutliers[0]
        pos_opp2 = pos_target - outlier_range + nearby_opp_suboutliers[1]
        amount = min(abs(o_delta[pos_target] / 2), abs(o_delta[pos_opp1]), abs(o_delta[pos_opp2]))
        return pos_opp1, pos_opp2, amount
    else:
        return None, None, None


def orientation_outlier_removal(
        data: Dict[str, pd.DataFrame], o_col: str, do_col: str,
        outlier_range: int = 4, outlier_th: float = 90., suboutlier_th: float = 45.
) -> None:
    """ Integral-preserved orientation outlier removal

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        o_col (str): The column name of subject orientation
        do_col (str): The column name to store the subject orientation after outlier removal
        outlier_range (int): The search range for outlier removal
        outlier_th (float): The threshold to determine whether orientation velocity is considered as an outlier in degree
        suboutlier_th (float): The threshold to determine whether orientation velocity is considered as a suboutlier in degree
    """

    for i, (sid, sdata) in enumerate(data.items(), 1):
        o = sdata[o_col].to_numpy() * 180. / np.pi
        o_delta = (o[1:] - o[:-1] + 540) % 360 - 180
        pos_outliers = np.where(o_delta > outlier_th, [1] * len(o_delta), [0] * len(o_delta))
        neg_outliers = np.where(o_delta < -outlier_th, [1] * len(o_delta), [0] * len(o_delta))
        pos_suboutliers = np.where(
            (o_delta > suboutlier_th) & (o_delta <= outlier_th), [1] * len(o_delta), [0] * len(o_delta)
        )
        neg_suboutliers = np.where(
            (o_delta >= -outlier_th) & (o_delta < -suboutlier_th), [1] * len(o_delta), [0] * len(o_delta)
        )

        do_delta = o_delta.copy()
        for t in tqdm(
                range(len(o_delta)), total=len(o_delta),
                desc='Outlier Removal for {} ({}/{})'.format(sid, i, len(data))
        ):
            while pos_outliers[t] == 1:  # Remove positive outliers
                t_opp, amount = _find_nearest_opposite_outlier(o_delta, neg_outliers, t, outlier_range)
                if t_opp is not None:  # An outlier in the opposite direction is found
                    do_delta[t] -= amount
                    do_delta[t_opp] += amount
                    neg_outliers[t_opp] = do_delta[t_opp] < -outlier_th
                    neg_suboutliers[t_opp] = -outlier_th <= do_delta[t_opp] < -suboutlier_th
                else:
                    t_opp1, t_opp2, amount = _find_nearest_opposite_suboutliers(
                        o_delta, neg_suboutliers, t, outlier_range
                    )
                    if t_opp1 is not None:
                        do_delta[t] -= amount * 2
                        do_delta[t_opp1] += amount
                        do_delta[t_opp2] += amount
                        neg_outliers[t_opp1] = do_delta[t_opp1] < -outlier_th
                        neg_suboutliers[t_opp1] = -outlier_th <= do_delta[t_opp1] < -suboutlier_th
                        neg_outliers[t_opp2] = do_delta[t_opp2] < -outlier_th
                        neg_suboutliers[t_opp2] = -suboutlier_th <= do_delta[t_opp2] < -suboutlier_th
                    else:
                        break

                pos_outliers[t] = do_delta[t] > outlier_th
                pos_suboutliers[t] = suboutlier_th < do_delta[t] <= outlier_th

            while neg_outliers[t] == 1:  # Remove negative outliers
                t_opp, amount = _find_nearest_opposite_outlier(o_delta, pos_outliers, t, outlier_range)
                if t_opp is not None:  # An outlier in the opposite direction is found
                    do_delta[t] += amount
                    do_delta[t_opp] -= amount
                    pos_outliers[t_opp] = do_delta[t_opp] > outlier_th
                    pos_suboutliers[t] = suboutlier_th < do_delta[t_opp] <= outlier_th
                else:
                    t_opp1, t_opp2, amount = _find_nearest_opposite_suboutliers(
                        o_delta, pos_suboutliers, t, outlier_range
                    )
                    if t_opp1 is not None:
                        do_delta[t] += amount * 2
                        do_delta[t_opp1] -= amount
                        do_delta[t_opp2] -= amount
                        pos_outliers[t_opp1] = do_delta[t_opp1] > outlier_th
                        pos_suboutliers[t_opp1] = suboutlier_th < do_delta[t_opp1] <= outlier_th
                        pos_outliers[t_opp2] = do_delta[t_opp2] > outlier_th
                        pos_suboutliers[t_opp2] = suboutlier_th < do_delta[t_opp2] <= outlier_th
                    else:
                        break

                neg_outliers[t] = do_delta[t] < -outlier_th
                neg_suboutliers[t] = -outlier_th <= do_delta[t] < -suboutlier_th

        do = [o[0] / 180. * np.pi]
        for v, ot in zip(do_delta, o[:-1]):
            if np.isnan(do[-1]) and np.isnan(v):
                do.append(np.nan)
            else:
                if np.isnan(do[-1]):
                    do[-1] = ot / 180. * np.pi
                do.append(do[-1] + v / 180. * np.pi)
        sdata[do_col] = do


def kf_based_motion_filtering(
        data: Dict[str, pd.DataFrame], o_col: str, d2d_col: str, do_col: str, dd2d_col: str,
        t_delta: timedelta = timedelta(microseconds=100000),
        ubi_err_std: float = 0.3, dis_phi: float = 5e-5, dis_min: float = 0.1,
        o_err_std: float = 1., o_phi0: float = 1e-2, o_std_scale: float = 0.5, o_q_scale_factor: float = 1e-3
) -> None:
    """ Motion data denoising based on Kalman Filter (KF)

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        o_col (str): The column name of subject orientation
        d2d_col (str): The column name of distance between left and right tags in 2D space
        do_col (str): The column name to store the subject orientation after denoising
        dd2d_col (str): The column name to store the distance between left and right tags in 2D space after denoising
        t_delta (timedelta): Time difference between adjacent data points after interpolation
        ubi_err_std (float): STD of Ubisense measurement error
        dis_phi (float): Initial process noise of distance between left and right tags in 2D space
        dis_min (float): Minimal allowed distance between left and right tags in 2D space
        o_err_std (float): STD of subject orientation measurement error
        o_phi0 (float): Initial process noise of subject orientation
        o_std_scale (float): Scaling factor of subject orientation STD
        o_q_scale_factor (float): Scaling factor of subject orientation process noise
    """

    dt = t_delta.total_seconds()
    f0 = np.array([[1., dt, 0.5 * dt * dt],
                   [0, 1., dt],
                   [0, 0, 1.]], dtype=float)

    for i, (sid, sdata) in enumerate(data.items(), 1):
        # Apply KF filters on subject orientation and distance between left and right tags in 2D space
        need_init = True
        dd2d, do = list(), list()
        o_last, o_count, o_phi = 0, 0, o_phi0
        for rid, row in tqdm(
                sdata.iterrows(), total=len(sdata),
                desc='KF-based motion denoising for {} ({}/{})'.format(sid, i, len(data))
        ):
            if need_init:
                # Initialize Kalman Filter
                kf_dis = KalmanFilter(2, 1)
                kf_dis.x = np.array([sdata[d2d_col][0], 0.])
                kf_dis.P *= 3
                kf_dis.R *= (ubi_err_std * 2) ** 2
                kf_dis.F = np.array([[1., dt], [0, 1.]], dtype=float)
                kf_dis.H = np.array([[1., 0]], dtype=float)
                kf_dis.Q = Q_discrete_white_noise(2, dt=dt, var=dis_phi)

                kf_o = KalmanFilter(3, 1)
                kf_o.x = np.array([sdata[o_col][0] * 180. / np.pi, 0., 0.])
                kf_o.P *= 3
                kf_o.R *= o_err_std ** 2
                kf_o.F = f0.copy()
                kf_o.H = np.array([[1, 0, 0]], dtype=float)
                kf_o.Q = Q_discrete_white_noise(3, dt=dt, var=o_phi0)

                need_init = False

            if pd.isnull(row[d2d_col]) or pd.isnull(row[o_col]):
                need_init = True
                o_last, o_count, o_phi = 0, 0, o_phi0
                dd2d.append(np.nan)
                do.append(np.nan)
            else:
                kf_dis.predict()
                kf_dis.update(row[d2d_col])
                dd2d.append(kf_dis.x[0] if kf_dis.x[0] > dis_min else dis_min)

                kf_o.predict()
                kf_o.update(row[o_col] * 180. / np.pi)
                do.append(((kf_o.x[0] + 540) % 360 - 180) / 180. * np.pi)

                std = np.sqrt(kf_o.S)[0, 0]
                if abs(kf_o.y[0]) > o_std_scale * std and o_count < 3:
                    o_phi += o_q_scale_factor
                    kf_o.Q = Q_discrete_white_noise(3, dt, o_phi)
                    o_count += 1
                elif o_count > 0:
                    o_phi -= o_q_scale_factor
                    kf_o.Q = Q_discrete_white_noise(3, dt, o_phi)
                    o_count -= 1

        sdata[dd2d_col] = dd2d
        sdata[do_col] = do


def denoise_motion_data(
        data: Dict[str, pd.DataFrame], o_col: str, d2d_col: str, cx_col: str, cy_col: str, do_col: str, dd2d_col: str,
        dlx_col: str, dly_col: str, drx_col: str, dry_col: str, t_delta: timedelta = timedelta(microseconds=100000),
        outlier_range: int = 4, outlier_th: float = 90., suboutlier_th: float = 45.,
        ubi_err_std: float = 0.3, dis_phi: float = 5e-5, dis_min: float = 0.1,
        o_err_std: float = 1., o_phi0: float = 1e-2, o_std_scale: float = 0.5, o_q_scale_factor: float = 1e-3
) -> None:
    """ Denoise the motion data collected from the classrooms

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        o_col (str): The column name of subject orientation
        d2d_col (str): The column name of distance between left and right tags in 2D space
        cx_col (str): The column name of cx
        cy_col (str): The column name of cy
        do_col (str): The column name to store the subject orientation after denoising
        dd2d_col (str): The column name to store the distance between left and right tags in 2D space after denoising
        dlx_col (str): The column name to store the lx after denoising
        dly_col (str): The column name to store the ly after denoising
        drx_col (str): The column name to store the rx after denoising
        dry_col (str): The column name to store the ry after denoising
        t_delta (timedelta): Time difference between adjacent data points after interpolation
        outlier_range (int): The search range for outlier removal
        outlier_th (float): The threshold to determine whether orientation velocity is considered as an outlier in degree
        suboutlier_th (float): The threshold to determine whether orientation velocity is considered as a suboutlier in degree
        ubi_err_std (float): STD of Ubisense measurement error
        dis_phi (float): Initial process noise of distance between left and right tags in 2D space
        dis_min (float): Minimal allowed distance between left and right tags in 2D space
        o_err_std (float): STD of subject orientation measurement error
        o_phi0 (float): Initial process noise of subject orientation
        o_std_scale (float): Scaling factor of subject orientation STD
        o_q_scale_factor (float): Scaling factor of subject orientation process noise
    """

    orientation_outlier_removal(data, o_col, do_col, outlier_range, outlier_th, suboutlier_th)
    compute_left_and_right_tag_positions(data, cx_col, cy_col, do_col, d2d_col, dlx_col, dly_col, drx_col, dry_col)
    kf_based_motion_filtering(
        data, do_col, d2d_col, do_col, dd2d_col,
        t_delta, ubi_err_std, dis_phi, dis_min, o_err_std, o_phi0, o_std_scale, o_q_scale_factor
    )
    compute_left_and_right_tag_positions(data, cx_col, cy_col, do_col, dd2d_col, dlx_col, dly_col, drx_col, dry_col)


def integrate_vocalization_data(data: Dict[str, pd.DataFrame], mapping: pd.DataFrame, its_dir: Path) -> None:
    """ Integrate vocalization data from its files into motion data

    Args:
        data (Dict[str, pd.DataFrame]): The interpolated classroom motion data grouped by subjects
        mapping (pd.DataFrame): The classroom metadata generated by :func:load_mapping_file
        its_dir (Path): The path to the directory where its files locate
    """

    # Generate the mapper from LENA DLP ID to subject ID and subject type
    lena_id_mapper = {row["LENA"]: (sid, row["Type"]) for sid, row in mapping.iterrows()}

    # Time string parser for its files
    def parse_its_time(s: str):
        return float(s[2:-1])

    # Integrate data from each its file into the motion data of corresponding subject
    its_files = list(sorted(its_dir.iterdir()))
    for i, its_file in enumerate(its_files, 1):
        lena_id = int(its_file.stem.split("_")[-1])
        if lena_id not in lena_id_mapper:
            continue  # LENA ID is not in the mapping, drop the its file
        sub_id, sub_type = lena_id_mapper[lena_id]
        if sub_id not in data:
            logging.warning("No motion data available for {}. The its file \"{}\" is ignored.".format(sub_id, its_file))
            continue
        sdata = data[sub_id]
        # Initialize the column "vocal" with no vocalization (0)
        sdata["chn_vocal"] = 0
        sdata["chf_vocal"] = 0
        sdata["adult_vocal"] = 0
        sdata["chn_vocal_average_dB"] = 0
        sdata["chf_vocal_average_dB"] = 0
        sdata["adult_vocal_average_dB"] = 0
        sdata["chn_vocal_peak_dB"] = 0
        sdata["chf_vocal_peak_dB"] = 0
        sdata["adult_vocal_peak_dB"] = 0

        tree = ET.parse(its_file)
        rt = tree.getroot()

        # Obtain the start time of the its file (Assume that time in the motion data uses US East timezone)
        # TODO: Allow for custom timezone
        rec = next(rt.iter("Recording"))
        ref_time = datetime.fromisoformat(rec.attrib["startClockTime"][:-1])
        if 3 <= ref_time.month <= 11:
            ref_time -= timedelta(hours=4)
        else:
            ref_time -= timedelta(hours=5)
        ref_time -= timedelta(seconds=parse_its_time(rec.attrib["startTime"]))

        # Obtain all the valid segments
        segments_chn, segments_chf, segments_adult = list(), list(), list()
        for rec in rt.iter("Recording"):
            for segment in rec.iter("Segment"):
                seg = segment.attrib
                # Only consider its files from children and teachers
                if sub_type == CHILD_SUBJECT or sub_type == TEACHER_SUBJECT:
                    if seg["spkr"] == "CHN" and seg["childUttCnt"] != "0":
                        for uid in range(1, int(seg["childUttCnt"]) + 1):
                            # TODO: Assume that the motion data are rounded to tenth of second. Allow for custom t_delta
                            start_time = timedelta(seconds=np.ceil(parse_its_time(seg["startUtt{}".format(uid)]) * 10.) / 10.)
                            end_time = timedelta(seconds=np.floor(parse_its_time(seg["endUtt{}".format(uid)]) * 10.) / 10.)
                            segments_chn.append((ref_time + start_time, ref_time + end_time, seg['average_dB'], seg['peak_dB']))
                    else:
                        start_time = timedelta(seconds=np.ceil(parse_its_time(seg["startTime"]) * 10.) / 10.)
                        end_time = timedelta(seconds=np.floor(parse_its_time(seg["endTime"]) * 10.) / 10.)
                        if seg["spkr"] == "CHF":
                            segments_chf.append((ref_time + start_time, ref_time + end_time, seg['average_dB'], seg['peak_dB']))
                        elif seg["spkr"] in ["FAN", "MAN"]:
                            segments_adult.append((ref_time + start_time, ref_time + end_time, seg['average_dB'], seg['peak_dB']))

        # Integrate vocalization data
        seg_id = {'chn_vocal': 0, 'chf_vocal': 0, 'adult_vocal': 0}
        count = 0
        progress = tqdm(
            sdata.iterrows(), total=len(sdata),
            desc="Integrate vocalization data from its file \"{}\" ({}/{})".format(its_file, i, len(its_files)),
        )
        for rid, row in progress:
            for segments, cname in [(segments_chn, 'chn_vocal'), (segments_chf, 'chf_vocal'), (segments_adult, 'adult_vocal')]:
                while seg_id[cname] < len(segments) and row["Time"] > segments[seg_id[cname]][1]:
                    seg_id[cname] += 1

                if seg_id[cname] >= len(segments):
                    progress.update(len(sdata) - progress.n)
                    break  # all the segments have been processed

                if segments[seg_id[cname]][0] <= row["Time"] <= segments[seg_id[cname]][1]:
                    sdata.loc[rid, cname] = 1
                    sdata.loc[rid, cname + '_average_dB'] = segments[seg_id[cname]][2]
                    sdata.loc[rid, cname + '_peak_dB'] = segments[seg_id[cname]][3]
                    count += 1
        progress.close()
        logging.info("{} vocalizations are found overlapped with motion data".format(count))


def prepare_classroom_motion_vocal_dataset(
        mf_path: Path, mdf_path: Path, its_dir: Path, o_path: Path = None
) -> Dict[str, pd.DataFrame]:
    """ Generate the dataset of motion and vocalization data in one classroom observation (one day)

    Args:
        mf_path (Path): The filepath to the mapping file
        mdf_path (Path): The filepath to the motion data file
        its_dir (Path): The path to the directory where its files locate
        o_path (Path): The filepath to store the generated dataset

    Returns:
        Dict[str, pd.DataFrame]: a Dict mapping subject ID to the corresponding motion and vocalization data
    """

    mapping = load_mapping_file(mf_path)
    logging.warning("The length of mapping is")
    mapping_length = mapping.size
    logging.warning("The length of mapping is:"+   str(mapping_length))
    dataset = load_motion_data_file(mdf_path, mapping)
    logging.warning("The length of dataset is:"+   str(len(str(dataset))))
    dataset = pair_and_interpolate_motion_data(dataset)
    logging.info("Subjects contained in the dataset: [{}]".format(", ".join(dataset)))

    compute_subject_orientation(dataset, "lx", "ly", "rx", "ry", "o")
    compute_tag_2d_distance(dataset, "lx", "ly", "rx", "ry", "dis2d")
    compute_tag_center_position(dataset, "lx", "ly", "lz", "rx", "ry", "rz", "cx", "cy", "cz")
    denoise_motion_data(dataset, "o", "dis2d", "cx", "cy", "o_kf", "dis2d_kf", "lx_kf", "ly_kf", "rx_kf", "ry_kf")
    compute_tag_center_2d_position(dataset, "lx_kf", "ly_kf", "rx_kf", "ry_kf", "cx_kf", "cy_kf")

    integrate_vocalization_data(dataset, mapping, its_dir)

    if o_path is not None:
        pickle.dump({"mapping": mapping, "classroom_observation_dataset": dataset}, open(str(o_path), "wb"))
        for sub, sdata in dataset.items():
            sdata.to_csv(o_path.parent / '{}_{}.csv'.format(o_path.stem, sub), index=False)

    return dataset


if __name__ == "__main__":
    logging.basicConfig(level=logging.INFO)

    parser = ArgumentParser(
        description="Generate the dataset of motion and vocalization data in one classroom observation (one day)"
    )
    parser.add_argument("mapping_file_path", type=str, help="The filepath to the mapping file")
    parser.add_argument("motion_data_file_path", type=str, help="The filepath to the motion data file")
    parser.add_argument("its_file_dir", type=str, help="The path to the directory where its files locate")
    parser.add_argument("output_path", type=str, help="The filepath to store the generated dataset")
     
    args = parser.parse_args()
    logging.warning("args.mapping_file_path "+args.mapping_file_path)
    logging.warning("args.motion_data_file_path "+args.motion_data_file_path)
    logging.warning("args.its_file_dir "+args.its_file_dir)
    prepare_classroom_motion_vocal_dataset(
        Path(args.mapping_file_path),
        Path(args.motion_data_file_path),
        Path(args.its_file_dir),
        Path(args.output_path)
    )
